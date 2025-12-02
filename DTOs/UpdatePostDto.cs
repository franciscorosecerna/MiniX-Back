using System.ComponentModel.DataAnnotations;

namespace MiniX.Backend.DTOs
{
    public class UpdatePostDto
    {
        [MaxLength(280, ErrorMessage = "El contenido no puede exceder 280 caracteres")]
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public IFormFile? Image { get; set; }
    }
}
