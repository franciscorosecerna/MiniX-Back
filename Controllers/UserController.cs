using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniX.Backend.DTOs;
using MiniX.Backend.Models;
using MiniX.Backend.Services;
using System.Security.Claims;

namespace MiniX.Backend.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private string? GetCurrentUserId()
            => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private UserResponseDto MapToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Bio = user.Bio,
                ImageUrl = user.ProfileImageUrl,
                ProfileImageUrl = user.ProfileImageUrl,
                FollowersCount = user.FollowersCount,
                FollowingCount = user.FollowingCount,
                CreatedAt = user.CreatedAt
            };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            return Ok(MapToDto(user));
        }

        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            return Ok(MapToDto(user));
        }

        [HttpGet("email/{email}")]
        [Authorize]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var currentUserId = GetCurrentUserId();
            var user = await _userService.GetUserByEmailAsync(email);

            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            if (user.Id != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            return Ok(MapToDto(user));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string id, [FromForm] UpdateUserRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id && !User.IsInRole("Admin"))
                return Forbid();

            var result = await _userService.UpdateUserAsync(id, request);

            if (!result)
                return BadRequest(new { message = "No se realizaron cambios" });

            return NoContent();
        }

        [HttpPut("{id}/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
                return Forbid();

            var result = await _userService.ChangePasswordAsync(id, request.CurrentPassword, request.NewPassword);
            if (!result)
                return BadRequest(new { message = "No se pudo cambiar la contraseña" });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id && !User.IsInRole("Admin"))
                return Forbid();

            var user = await _userService.GetUserByIdAsync(id);
            if (user is null)
                return NotFound(new { message = "Usuario no encontrado" });

            if (user.Role == "Admin")
                return BadRequest(new { message = "Git gud"});

            _ = await _userService.DeleteUserAsync(id);
            return NoContent();
        }

        [HttpGet("check-username/{username}")]
        public async Task<IActionResult> CheckUsernameAvailability(string username)
        {
            var isAvailable = await _userService.IsUsernameAvailableAsync(username);
            return Ok(new { available = isAvailable });
        }

        [HttpGet("check-email/{email}")]
        public async Task<IActionResult> CheckEmailAvailability(string email)
        {
            try
            {
                var isAvailable = await _userService.IsEmailAvailableAsync(email);
                return Ok(new { available = isAvailable });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/follow")]
        [Authorize]
        public async Task<IActionResult> FollowUser(string id)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            try
            {
                var result = await _userService.FollowUserAsync(currentUserId, id);
                return Ok(result);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}/follow")]
        [Authorize]
        public async Task<IActionResult> UnfollowUser(string id)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            try
            {
                var result = await _userService.UnfollowUserAsync(currentUserId, id);
                return Ok(result);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/is-following")]
        [Authorize]
        public async Task<IActionResult> IsFollowing(string id)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var isFollowing = await _userService.IsFollowingAsync(currentUserId, id);
            return Ok(new { isFollowing });
        }

        [HttpGet("{id}/followers")]
        public async Task<IActionResult> GetFollowers(string id, [FromQuery] int skip = 0, [FromQuery] int limit = 20)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var totalCount = await _userService.GetFollowersCountAsync(id);
            var followers = await _userService.GetFollowersAsync(id, skip, limit);
            var response = followers.Select(MapToDto).ToList();
            return Ok(new { response, totalCount });
        }

        [HttpGet("{id}/following")]
        public async Task<IActionResult> GetFollowing(string id, [FromQuery] int skip = 0, [FromQuery] int limit = 20)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var totalCount = await _userService.GetFollowingCountAsync(id);
            var following = await _userService.GetFollowingAsync(id, skip, limit);
            var response = following.Select(MapToDto).ToList();
            return Ok(new { response ,totalCount });
        }

        [HttpGet("{id}/followers/count")]
        public async Task<IActionResult> GetFollowersCount(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var count = await _userService.GetFollowersCountAsync(id);
            return Ok(new { count });
        }

        [HttpGet("{id}/following/count")]
        public async Task<IActionResult> GetFollowingCount(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var count = await _userService.GetFollowingCountAsync(id);
            return Ok(new { count });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int limit = 10)
        {
            var users = await _userService.SearchUsersAsync(q, limit);
            var response = users.Select(MapToDto).ToList();
            return Ok(response);
        }

        [HttpGet("{id}/validate")]
        public async Task<IActionResult> ValidateUser(string id)
        {
            var isValid = await _userService.ValidateUserAsync(id);
            return Ok(new { isValid });
        }
    }
}
