using System.ComponentModel.DataAnnotations;

namespace MiniX.Backend.DTOs
{
    public class CreatePostDto
    {
        [MaxLength(280, ErrorMessage = "El contenido no puede exceder 280 caracteres")]
        public string Content { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }

        public string? ParentPostId { get; set; }
    }
}