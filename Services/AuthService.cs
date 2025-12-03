using Microsoft.IdentityModel.Tokens;
using MiniX.Backend.Models;
using MiniX.Backend.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniX.Backend.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password, string displayName);
        Task<(bool Success, string? AccessToken, string? RefreshToken, string? Displayname, string? UserName, string? ImageUrl, bool IsAdmin, string Message)> LoginAsync(string username, string password);
        Task<(bool Success, string? AccessToken, string? RefreshToken, string Message)> RefreshAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string userId, string refreshToken);
        Task<bool> RevokeAllTokensAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository users, IConfiguration config, ILogger<AuthService> logger)
        {
            _users = users;
            _config = config;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password, string displayName)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 20)
                return (false, "El nombre de usuario debe tener entre 3 y 20 caracteres.");

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
                return (false, "El nombre de usuario solo puede contener letras, números y guiones bajos.");

            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                return (false, "El email no es válido.");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return (false, "La contraseña debe tener al menos 8 caracteres.");

            if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 50)
                return (false, "El nombre de visualización debe tener máximo 50 caracteres.");

            if (await _users.UsernameExistsAsync(username.ToLower()))
                return (false, "El nombre de usuario ya existe.");

            if (await _users.EmailExistsAsync(email.ToLower()))
                return (false, "El email ya está registrado.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Username = username.ToLower(),
                Email = email.ToLower(),
                DisplayName = displayName,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                RefreshTokens = []
            };

            //Admin
            if(user.Username == "fedpo")
            {
                user.Role = "Admin";
            }

            await _users.CreateAsync(user);

            _logger.LogInformation("Usuario registrado: {Username}", username);

            return (true, "Usuario registrado correctamente.");
        }

        public async Task<(bool Success, string? AccessToken, string? RefreshToken, 
            string? Displayname, string? UserName, string? ImageUrl, bool IsAdmin, string Message)>
            LoginAsync(string username, string password)
        {
            username = username.Trim().ToLower();

            var user = await _users.GetByUsernameAsync(username);

            var passwordForDummy = user?.PasswordHash ?? "$2a$11$dummyhashfordummypasswordverification";
            var validPassword = BCrypt.Net.BCrypt.Verify(password, passwordForDummy);

            if (user == null || !validPassword)
            {
                _logger.LogWarning("Login fallido para: {Username}", username);
                return (false, null, null, null, null, null, false, "Credenciales inválidas.");
            }

            bool isAdmin = false;
            if (user!.Role == "Admin")
            {
                isAdmin = true;
            }

            //Admin (change not persisted)
            if (user.Username == "fedpo")
            {
                user.Role = "Admin";
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = CreateRefreshToken();

            await _users.AddRefreshTokenAsync(user.Id!, refreshToken);

            return (true, accessToken, refreshToken.PlainToken!, user.DisplayName, user.Username, user.ProfileImageUrl, isAdmin, "Login exitoso.");
        }


        public async Task<(bool Success, string? AccessToken, string? RefreshToken, string Message)>
            RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return (false, null, null, "Refresh token inválido.");

            var hashed = HashToken(refreshToken);

            var user = await _users.GetByRefreshTokenAsync(hashed);

            if (user == null)
            {
                _logger.LogWarning("Refresh inválido (no coincide con DB)");
                return (false, null, null, "Refresh token inválido o expirado.");
            }

            await _users.RevokeRefreshTokenAsync(user.Id!, hashed);

            var newRefresh = CreateRefreshToken();
            await _users.AddRefreshTokenAsync(user.Id!, newRefresh);

            var newJwt = GenerateJwtToken(user);

            return (true, newJwt, newRefresh.PlainToken!, "Token renovado.");
        }

        public async Task<bool> RevokeTokenAsync(string userId, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(userId))
                return false;

            try
            {
                var hashed = HashToken(refreshToken);
                var revoked = await _users.RevokeRefreshTokenAsync(userId, hashed);

                if (revoked)
                {
                    _logger.LogInformation("Refresh token revocado para usuario: {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("No se pudo revocar el refresh token para usuario: {UserId}", userId);
                }

                return revoked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar refresh token para usuario: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RevokeAllTokensAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            try
            {
                var user = await _users.GetByIdAsync(userId);
                if (user == null)
                    return false;

                var revokedTokens = user.RefreshTokens
                    .Where(rt => rt.RevokedAt == null)
                    .Select(rt =>
                    {
                        rt.RevokedAt = DateTime.UtcNow;
                        return rt;
                    })
                    .ToList();

                revokedTokens.AddRange(user.RefreshTokens.Where(rt => rt.RevokedAt != null));

                var success = await _users.ReplaceRefreshTokensAsync(userId, revokedTokens);

                if (success)
                {
                    _logger.LogInformation("Todos los refresh tokens revocados para usuario: {UserId}", userId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar todos los tokens para usuario: {UserId}", userId);
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("username", user.Username),
                new Claim("displayName", user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static RefreshToken CreateRefreshToken()
        {
            var plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var hashed = HashToken(plain);

            return new RefreshToken
            {
                Token = hashed,
                PlainToken = plain,
                CreatedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7)
            };
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}