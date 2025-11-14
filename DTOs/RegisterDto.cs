using System.Diagnostics.CodeAnalysis;

namespace MiniX.Backend.DTOs
{
    public class RegisterDto
    {
        [NotNull]
        public string Username { get; set; } = null!;

        [NotNull]
        public string Email { get; set; } = null!;

        [NotNull]
        public string Password { get; set; } = null!;

        [NotNull]
        public string DisplayName { get; set; } = null!;
    }
}
