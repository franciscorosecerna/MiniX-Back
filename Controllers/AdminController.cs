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
        public AdminController(IUserService userService, IPostService postService)
        {
            _userService = userService;
            _postService = postService;
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
                PostsCount = count
            };
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var users = await _userService.GetUsersAsync(page, pageSize);

            List<UserResponseDto> response = [];
            foreach (var user in users)
            {
                var x = await _postService.GetUserPostsCountAsync(user.Id);
                response.Add(MapToDto(user, x));
            }
            return Ok(response);
        }

        [HttpPost("password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserPasswordByUsername([FromBody] PasswordResetDto dto )
        {
            if (dto.id == "") return BadRequest(new { message = "No esta definido el usuario, ¿Habra un bug en la pagina?" });
            if (dto.newpass == "" || dto.newpass.Length<8) return BadRequest(new { message = "No se ingreso una contraseña valida" });

            var ret = await _userService.PasswordResetAsync(dto.id, dto.newpass);
            return Ok(ret);
        }
    }
}
