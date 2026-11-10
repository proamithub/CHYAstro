SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

UPDATE tbl_Users
SET PasswordHash = '$2a$11$qv98cEf7E3TmHEKWwSV0qu0qZqrz9Dq9i58Qpc1/Fkwv7y5CJp2p.'
WHERE Email = 'admin@cyhastro.com';

SELECT Email, Role, LEN(PasswordHash) AS HashLength, PasswordHash
FROM tbl_Users
WHERE Email = 'admin@cyhastro.com';
