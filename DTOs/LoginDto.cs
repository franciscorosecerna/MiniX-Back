using System.ComponentModel.DataAnnotations;

namespace MiniX.Backend.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "El nombre de usuario solo puede contener letras, números y guiones bajos")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "La contraseña no puede estar vacía")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}