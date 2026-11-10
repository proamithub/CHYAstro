using BCrypt.Net;
using CyhAstro.Core.DTOs;
using CyhAstro.Core.Entities;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CyhAstro.Infrastructure.Repositories;

/// <summary>
/// User repository — IUserRepository interface implement karta hai.
/// Saare DB calls Stored Procedures se hote hain via Dapper.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _db;

    public UserRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<(int ResultCode, Guid? UserId)> RegisterOrLoginAsync(RegisterRequest req)
    {
        using var conn = _db.CreateConnection();

        // Password hash karo (BCrypt)
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);

        var p = new DynamicParameters();
        p.Add("@FullName",        req.FullName);
        p.Add("@Email",           req.Email);
        p.Add("@PhoneNumber",     req.PhoneNumber);
        p.Add("@PasswordHash",    hashedPassword);
        p.Add("@Gender",          req.Gender);
        p.Add("@DateOfBirth",     req.DateOfBirth.ToDateTime(TimeOnly.MinValue));
        p.Add("@TimeOfBirth",     req.TimeOfBirth?.ToTimeSpan());
        p.Add("@BirthCity",       req.BirthCity);
        p.Add("@BirthLatitude",   req.BirthLatitude);
        p.Add("@BirthLongitude",  req.BirthLongitude);
        p.Add("@DeviceToken",     req.DeviceToken);
        p.Add("@ResultCode",      dbType: DbType.Int32,  direction: ParameterDirection.Output);
        p.Add("@UserId",          dbType: DbType.Guid,   direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            "sp_User_RegisterOrLogin", p,
            commandType: CommandType.StoredProcedure);

        return (p.Get<int>("@ResultCode"), p.Get<Guid?>("@UserId"));
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<UserProfileDto>(
            "sp_User_GetProfile",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateDeviceTokenAsync(Guid userId, string token)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE tbl_Users SET DeviceToken = @Token, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId",
            new { UserId = userId, Token = token });
    }

    // Password verify helper (login ke liye)
    public bool VerifyPassword(string plainPassword, string storedHash)
        => BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);

    // Raw password hash get karne ke liye (login verify)
    public async Task<(string? PasswordHash, Guid? UserId, string? Role, bool IsBlocked)> GetLoginDataAsync(string? email, string? phone)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync(
            @"SELECT UserId, PasswordHash, Role, IsBlocked
              FROM tbl_Users
              WHERE (@Email IS NULL OR Email = @Email)
                AND (@Phone IS NULL OR PhoneNumber = @Phone)
                AND IsActive = 1",
            new { Email = email, Phone = phone });

        if (row == null) return (null, null, null, false);
        return (row.PasswordHash, (Guid?)row.UserId, (string?)row.Role, (bool)row.IsBlocked);
    }
}
