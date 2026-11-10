using CyhAstro.Core.DTOs;
using CyhAstro.Core.Interfaces;
using CyhAstro.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CyhAstro.Api.Controllers;

/// <summary>
/// Auth Controller — Register aur Login
/// Mobile app yahan se JWT token lega
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;
    private readonly ILocalizationService _localizer;

    public AuthController(IUserRepository userRepo, IConfiguration config, ILocalizationService localizer)
    {
        _userRepo  = userRepo;
        _config    = config;
        _localizer = localizer;
    }

    // ─────────────────────────────────────────────
    // POST /api/auth/register
    // ─────────────────────────────────────────────
    /// <summary>Naya user register karo. Email ya Phone mein se ek zaroori hai.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) && string.IsNullOrWhiteSpace(req.PhoneNumber))
            return BadRequest(new AuthResponse(false, _localizer.Get("MissingEmailOrPhone"), null, null, null, null));

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest(new AuthResponse(false, _localizer.Get("PasswordMinLength"), null, null, null, null));

        var (code, userId) = await _userRepo.RegisterOrLoginAsync(req);

        if (code == -1)
            return StatusCode(500, new AuthResponse(false, _localizer.Get("ServerError"), null, null, null, null));

        var profile = await _userRepo.GetProfileAsync(userId!.Value);
        var token   = GenerateJwtToken(userId.Value, profile!.FullName, "User");

        return Ok(new AuthResponse(
            Success: true,
            Message: code == 0 ? _localizer.Get("RegistrationSuccess") : _localizer.Get("LoginSuccess", profile.FullName),
            Token:   token,
            UserId:  userId,
            Role:    "User",
            Profile: profile));
    }

    // ─────────────────────────────────────────────
    // POST /api/auth/login
    // ─────────────────────────────────────────────
    /// <summary>Login with email/phone + password. JWT token milega.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) && string.IsNullOrWhiteSpace(req.PhoneNumber))
            return BadRequest(new AuthResponse(false, _localizer.Get("MissingEmailOrPhoneLogin"), null, null, null, null));

        // UserRepository mein cast karna padega extended method ke liye
        var userRepo = (UserRepository)_userRepo;
        var (hash, userId, role, isBlocked) = await userRepo.GetLoginDataAsync(req.Email, req.PhoneNumber);

        if (hash == null || userId == null)
            return Unauthorized(new AuthResponse(false, _localizer.Get("UserNotFound"), null, null, null, null));

        if (isBlocked)
            return Unauthorized(new AuthResponse(false, _localizer.Get("AccountBlocked"), null, null, null, null));

        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash))
            return Unauthorized(new AuthResponse(false, _localizer.Get("InvalidPassword"), null, null, null, null));

        // Device token update karo (push notifications ke liye)
        if (!string.IsNullOrWhiteSpace(req.DeviceToken))
            await _userRepo.UpdateDeviceTokenAsync(userId.Value, req.DeviceToken);

        var profile = await _userRepo.GetProfileAsync(userId.Value);
        var token   = GenerateJwtToken(userId.Value, profile!.FullName, role ?? "User");

        return Ok(new AuthResponse(
            Success: true,
            Message: _localizer.Get("LoginSuccess", profile.FullName),
            Token:   token,
            UserId:  userId,
            Role:    role,
            Profile: profile));
    }

    // ─────────────────────────────────────────────
    // GET /api/auth/profile
    // ─────────────────────────────────────────────
    /// <summary>Logged-in user ka profile + plan details lo.</summary>
    [HttpGet("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var profile = await _userRepo.GetProfileAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    // ─────────────────────────────────────────────
    // JWT Token Generator
    // ─────────────────────────────────────────────
    private string GenerateJwtToken(Guid userId, string fullName, string role)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry  = DateTime.UtcNow.AddDays(int.Parse(_config["JwtSettings:ExpiryDays"] ?? "30"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, fullName),
            new Claim(ClaimTypes.Role,              role),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             _config["JwtSettings:Issuer"],
            audience:           _config["JwtSettings:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
