using System.Diagnostics.CodeAnalysis;

namespace MiniX.Backend.DTOs
{
    public class RefreshTokenDto
    {
        [NotNull]
        public string RefreshToken { get; set; } = null!;
    }
}
