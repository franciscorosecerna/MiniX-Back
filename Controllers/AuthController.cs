using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniX.Backend.DTOs;
using MiniX.Backend.Services;

namespace MiniX.Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("fixed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto? dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Datos inválidos o ausentes." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(new
                {
                    message = string.Join(" | ", errors)
                });
            }

            var result = await _authService.RegisterAsync(
                dto.Username,
                dto.Email,
                dto.Password,
                dto.DisplayName
            );

            if (!result.Success)
            {
                _logger.LogWarning("Error al registrar usuario: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        public record RegisterSsoDto(string Username, string Email, string DisplayName,
                                     string token, string uid);
        [HttpPost("register/sso")]
        [AllowAnonymous]
        [EnableRateLimiting("fixed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterSso([FromBody] RegisterSsoDto? dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Datos inválidos o ausentes." });
            if (dto.token == "" || dto.uid == "") return Unauthorized();

            // Validación de Username
            if (string.IsNullOrWhiteSpace(dto.Username)) return BadRequest(new { message = "El nombre de usuario es requerido." });

            if (dto.Username.Length < 3 || dto.Username.Length > 50) return BadRequest(new { message = "El nombre de usuario debe tener entre 3 y 50 caracteres." });

            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Username, @"^[a-zA-Z0-9_]+$")) return BadRequest(new { message = "El nombre de usuario solo puede contener letras, números y guiones bajos." });

            // Validación de Email
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "El email es requerido." });

            if (dto.Email.Contains("@") && dto.Email.Length<4)
                return BadRequest(new { message = "El email no es valido" });

            // Validacion Displayname
            if (string.IsNullOrWhiteSpace(dto.DisplayName))
                return BadRequest(new { message = "El nombre para mostrar es requerido." });

            if (dto.DisplayName.Length < 1 || dto.DisplayName.Length > 100)
                return BadRequest(new { message = "El nombre para mostrar debe tener entre 1 y 100 caracteres." });

            // Llamada al servicio de autenticación
            var result = await _authService.RegisterSsoAsync(
                dto.Username,
                dto.Email,
                dto.uid,
                dto.token
            );

            if (!result.Success)
            {
                _logger.LogWarning("Error al registrar usuario SSO: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = "Se creo el usuario usando La cuenta de google, inicie sesion" });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("fixed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto? dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Datos inválidos o ausentes." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(dto.Username, dto.Password);

            if (!result.Success)
            {
                _logger.LogWarning("Intento fallido de login para: {Username}", dto.Username);
                return Unauthorized(new { message = result.Message });
            }

            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                SetRefreshTokenCookie(result.RefreshToken);

            return Ok(new
            {
                accessToken = result.AccessToken,
                message = result.Message,
                Url = result.ImageUrl,
                DisplayName = result.Displayname,
                Username = result.UserName,
                result.IsAdmin,
                isFirebase = false,
                Email = result.Email
            });
        }

        public record LoginSsoDto(string AccessToken, string uid);
        [HttpPost("login/sso")]
        [AllowAnonymous]
        [EnableRateLimiting("fixed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginSso([FromBody] LoginSsoDto? dto)
        {
            if (dto == null) {
                return BadRequest(new { message = "Datos inválidos o ausentes." });
            }

            var result = await _authService.LoginSsoAsync(dto.AccessToken, dto.uid);

            if (result == null) return BadRequest(new { message = "No se encontro usuario registrado." });

            return Ok(new
            {
                Url = result.ProfileImageUrl,
                DisplayName = result.DisplayName,
                Username = result.Username,
                IsAdmin = result.Role.Contains("Admin"),
                isFirebase = true,
                Email = result.Email
        });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto? dto)
        {
            string? refreshToken = dto?.RefreshToken ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized(new { message = "Refresh token no proporcionado." });

            var result = await _authService.RefreshAsync(refreshToken);

            if (!result.Success)
            {
                Response.Cookies.Delete("refreshToken");
                _logger.LogWarning("Refresh token inválido o expirado.");
                return Unauthorized(new { message = result.Message });
            }

            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                SetRefreshTokenCookie(result.RefreshToken);

            return Ok(new
            {
                accessToken = result.AccessToken,
                message = result.Message
            });
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(userId))
            {
                await _authService.RevokeTokenAsync(userId, refreshToken);
            }

            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "Sesión cerrada correctamente." });
        }

        [HttpPost("logout-all")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Token inválido." });

            var success = await _authService.RevokeAllTokensAsync(userId);

            Response.Cookies.Delete("refreshToken");

            if (!success)
                return StatusCode(500, new { message = "Error al cerrar todas las sesiones." });

            return Ok(new { message = "Todas las sesiones han sido cerradas." });
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Token inválido." });

            return Ok(new
            {
                id = userId,
                username = User.FindFirst("username")?.Value,
                displayName = User.FindFirst("displayName")?.Value
            });
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
