namespace MiniX.Backend.DTOs
{
    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string? Bio { get; set; } 
        public string? Email { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
