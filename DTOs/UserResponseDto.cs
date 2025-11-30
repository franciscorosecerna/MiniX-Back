namespace MiniX.Backend.DTOs
{
    public class UserResponseDto
    {
        public required string Id { get; set; }
        public required string Username { get; set; }
        public required string DisplayName { get; set; }
        public required string Email { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
