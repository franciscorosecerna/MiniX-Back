namespace MiniX.Backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniX.Backend.Services;
using MiniX.Emailer;

[ApiController]
public class ChangePasswordController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    public ChangePasswordController(IUserService userService, IAuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpPost("/api/password-reset/otp")]
    public IActionResult SendOtp([FromForm] string email) {
        if (!email.Contains("@")) return BadRequest("No existe un Email sin @");

        var usu = _userService.GetUserByEmailAsync(email).Result;
        if (usu == null) return BadRequest("no existe un usuario para ese email");

        var random = new Random();
        var otp = "";
        for (int i = 0; i < 6; i++) {
            otp += random.Next(0, 10);
        }

        var ret = _userService.SetOtp(otp, usu.Id);

        Task.Run(() => {
            var s = new OtpEmailSender();
            s.Send(email, usu.Username, otp);
        });

        return Ok(new { message = $"Se envio un email de recuperacion a {email}" });
    }

    public record CheckOtpDto(string otp = "", string email = "");
    [HttpPost("/api/password-reset/otp/check")]
    public IActionResult CheckOtp([FromForm] CheckOtpDto dto) {
        if (string.IsNullOrEmpty(dto.otp) || string.IsNullOrEmpty(dto.email)){
            return BadRequest("Todos los campos son requeridos");
        }

        var user = _userService.GetUserByEmailAsync(dto.email).Result;
        if (user == null) return BadRequest("No existe un usuario para ese email");

        var otp = user.Otp ?? "";

        if (otp != dto.otp){
            return BadRequest("El OTP es incorrecto o ha expirado");
        }

        return Ok();
    }

    public record ChangePassDto(string otp = "", string newpass = "", string email = "");
    [HttpPatch("/api/password-reset/change")]
    public IActionResult ChangePassword([FromForm] ChangePassDto dto) {
        if (string.IsNullOrEmpty(dto.otp) || string.IsNullOrEmpty(dto.newpass) || string.IsNullOrEmpty(dto.email)){
            return BadRequest("Todos los campos son requeridos");
        }

        if (dto.newpass.Length < 8) return BadRequest("La contraseña debe tener entre 8 y 100 caracteres");

        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.newpass, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9])[A-Za-z\d\W_]*$")) {
            return BadRequest("La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial");
        }

        var usu = _userService.GetUserByEmailAsync(dto.email).Result;
        if (usu == null) return BadRequest("no existe un usuario para ese email");

        var ret = _userService.PasswordResetAsync(usu.Id, dto.newpass).Result;

        if (!ret) {
            return BadRequest("No se pudo cambiar la contraseña");
        }

        var result = _authService.LoginAsync(usu.Username, dto.newpass).Result;

        if (!result.Success){
            return Unauthorized(result.Message);
        }

        if (!string.IsNullOrWhiteSpace(result.RefreshToken)){
            SetRefreshTokenCookie(result.RefreshToken);
        }

        ret = _userService.SetOtp(null, usu.Id);

        return ret ?
            Ok(new
            {
                accessToken = result.AccessToken,
                message = result.Message,
                Url = result.ImageUrl,
                DisplayName = result.Displayname,
                Username = result.UserName,
                result.IsAdmin
            }):
            BadRequest("No se pudo terminar el cambio de contraseña");
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
