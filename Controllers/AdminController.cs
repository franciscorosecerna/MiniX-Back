using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniX.Backend.DTOs;
using MiniX.Backend.Models;
using MiniX.Backend.Services;

namespace MiniX.Backend.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPostService _postService;
        private readonly IAuthService _authService;

        public AdminController(IUserService userService, IPostService postService, IAuthService authService)
        {
            _userService = userService;
            _postService = postService;
            _authService = authService;
        }

        private UserResponseDto MapToDto(User user, int count)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                FollowersCount = user.FollowersCount,
                FollowingCount = user.FollowingCount,
                CreatedAt = user.CreatedAt,
                PostsCount = count,
                IsAdmin = user.Role == "Admin" ? true : false,
            };
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue("sub")
                ?? HttpContext.Items["UserId"]?.ToString()
                ?? string.Empty;
        }

        [HttpGet("users")]
        [Authorize(Policy = "FirebaseOrDefault")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string q = "")
        {
            var isAdmin = await _authService.CheckAdmin(GetCurrentUserId());
            if (!isAdmin) {
                return Unauthorized();
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var users = await _userService.GetUsersAsync(page, pageSize, q);

            List<UserResponseDto> response = [];
            foreach (var user in users)
            {
                var x = await _postService.GetUserPostsCountAsync(user.Id);
                response.Add(MapToDto(user, x));
            }
            return Ok(response);
        }

        [HttpPost("password")]
        [Authorize(Policy = "FirebaseOrDefault")]
        public async Task<IActionResult> ChangeUserPasswordByUsername([FromBody] PasswordResetDto dto )
        {
            var isAdmin = await _authService.CheckAdmin(GetCurrentUserId());
            if (!isAdmin) {
                return Unauthorized();
            }

            if (dto.id == "") return BadRequest(new { message = "No esta definido el usuario, ¿Habra un bug en la pagina?" });
            if (dto.newpass == "" || dto.newpass.Length<8) return BadRequest(new { message = "No se ingreso una contraseña valida" });

            var ret = await _userService.PasswordResetAsync(dto.id, dto.newpass);
            return Ok(ret);
        }

        public record ToggleAdminDto(bool isAdmin, string id);
        [HttpPatch("give")]
        [Authorize(Policy = "FirebaseOrDefault")]
        public IActionResult ToggleUserAdmin([FromBody] ToggleAdminDto dto) {
            var isAdmin = _authService.CheckAdmin(GetCurrentUserId()).Result;
            if (!isAdmin) {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(dto.id)) {
                return BadRequest(new { message = "falta definir que usuario quiere modificar" });
            }

            var ret = _userService.ToggleAdmin(dto.isAdmin, dto.id);
            if (ret.Result) {
                return Ok(new { message = "Usuario modificado correctamente" });
            } else {
                return StatusCode(500, new { message = "Error al modificar el usuario" });
            }
        }
    }
}
