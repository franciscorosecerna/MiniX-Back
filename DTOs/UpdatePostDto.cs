using System.ComponentModel.DataAnnotations;

namespace MiniX.Backend.DTOs
{
    public class UpdatePostDto
    {
        [MaxLength(280, ErrorMessage = "El contenido no puede exceder 280 caracteres")]
        public string Content { get; set; } = string.Empty;

        [Url(ErrorMessage = "La URL de la imagen no es válida")]
        public string? ImageUrl { get; set; }
    }
}
