using System.Diagnostics.CodeAnalysis;

namespace MiniX.Backend.DTOs
{
    public class LoginDto
    {
        [NotNull]
        public string Username { get; set; } = null!;

        [NotNull]
        public string Password { get; set; } = null!;
    }
}
