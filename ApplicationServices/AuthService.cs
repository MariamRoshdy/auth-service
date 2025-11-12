using Microsoft.EntityFrameworkCore;
using UsersService.Data;
using UsersService.Dtos;
using System.Security.Cryptography;
using UsersService.Models;
using System.Text.RegularExpressions;
using UsersService.Helpers;
using Microsoft.Extensions.Options;
using UsersService.Data.Repositories;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace UsersService.ApplicationServices;

public class AuthService
{
    private readonly AppDBContext _context;
    private readonly JwtService _jwtService;
    private readonly JWT _jwtSettings;
    public AuthService(AppDBContext context,
     JwtService jwtService,
     IOptions<JWT> jwtSettings)
    {
        _context = context;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
    }
    public async Task RegisterUserAsync(RegisterDto dto)
    {
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already in use.");

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[A-Za-z]+$");
        if (!emailRegex.IsMatch(dto.Email))
            throw new Exception("Invalid email format.");

        // Hash the password
        using var hmac = new HMACSHA256();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User> VerifyUserAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");
        return user;
    }


    public async Task<LoginResponseDto> LoginAsync(string email, string password, string? ipAddress)
    {
        var user = await VerifyUserAsync(email, password);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Save refresh token to database
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress ?? string.Empty,
            UserId = user.Id
        };
        await _context.RefreshToken.AddAsync(refreshTokenEntity);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string accessToken, string refreshToken, string ipAddress)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // validate refresh token : to do
        var user = await _context.Users.FindAsync(userId) ??
            throw new SecurityTokenException("Invalid token");

        await RevokeTokenAsync(refreshToken, ipAddress, userId);

        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Save new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserId = user.Id
        };

        await _context.RefreshToken.AddAsync(newRefreshTokenEntity);

        return new LoginResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress, Guid userId)
    {
         var storedRefreshToken = await _context.RefreshToken.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (storedRefreshToken == null || storedRefreshToken.UserId != userId || !storedRefreshToken.IsActive)
            throw new SecurityTokenException("Invalid refresh token");

        // Revoke current refresh token
        storedRefreshToken.Revoked = DateTime.UtcNow;
        storedRefreshToken.RevokedByIp = ipAddress;
        await _context.RefreshToken
            .Where(r => r.Token == refreshToken)
            .ExecuteUpdateAsync(r => r
                .SetProperty(x => x.Revoked, DateTime.UtcNow)
                .SetProperty(x => x.RevokedByIp, ipAddress));
    }
}
