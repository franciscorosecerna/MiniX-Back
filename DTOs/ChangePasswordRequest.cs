using System.ComponentModel.DataAnnotations;

namespace MiniX.Backend.DTOs
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]*",
            ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
