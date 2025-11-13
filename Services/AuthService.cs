using Microsoft.IdentityModel.Tokens;
using MiniX.Backend.Models;
using MiniX.Backend.Repositories;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MiniX.Backend.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password, string displayName);
        Task<(bool Success, string? AccessToken, string? RefreshToken, string Message)> LoginAsync(string username, string password);
        Task<(bool Success, string? AccessToken, string? RefreshToken, string Message)> RefreshAsync(string refreshToken);
    }

    public class AuthService: IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository users, IConfiguration config)
        {
            _users = users;
            _config = config;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password, string displayName)
        {
            if (await _users.UsernameExistsAsync(username))
                return (false, "El nombre de usuario ya está en uso.");

            if (await _users.EmailExistsAsync(email))
                return (false, "El email ya está registrado.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                DisplayName = displayName,
                CreatedAt = DateTime.UtcNow
            };

            await _users.CreateAsync(user);

            return (true, "Usuario registrado correctamente.");
        }

        public async Task<(bool Success, string? AccessToken, string? RefreshToken, string Message)> LoginAsync(string username, string password)
        {
            var user = await _users.GetByUsernameAsync(username);

            if (user == null)
                return (false, null, null, "Usuario no encontrado.");

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, null, null, "Skill issue.");

            var accessToken = GenerateJwtToken(user);
            var refreshToken = CreateRefreshToken();

            await _users.AddRefreshTokenAsync(user.Id, refreshToken);

            return (true, accessToken, refreshToken.Token, "Login exitoso.");
        }

        public async Task<(bool Success, string? AccessToken, string? RefreshToken, string Message)> RefreshAsync(string refreshToken)
        {
            var user = await _users.GetAllAsync()
                .ContinueWith(t => t.Result.FirstOrDefault(u =>
                    u.RefreshTokens.Any(rt =>
                        rt.Token == refreshToken &&
                        rt.Expires > DateTime.UtcNow &&
                        rt.RevokedAt == null
                    )));

            if (user == null)
                return (false, null, null, "Refresh token inválido o expirado.");

            await _users.RevokeRefreshTokenAsync(user.Id, refreshToken);

            var newRefresh = CreateRefreshToken();
            await _users.AddRefreshTokenAsync(user.Id, newRefresh);

            var newAccessToken = GenerateJwtToken(user);

            return (true, newAccessToken, newRefresh.Token, "Token renovado.");
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim("username", user.Username),
                new Claim("displayName", user.DisplayName)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static RefreshToken CreateRefreshToken()
        {
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
