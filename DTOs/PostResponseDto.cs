using MiniX.Backend.Models;

namespace MiniX.Backend.DTOs
{
    public class PostResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string? AuthorImageUrl { get; set; } = string.Empty;
        public string AuthorDisplayName { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? ParentPostId { get; set; }
        public int LikesCount { get; set; }
        public int RepliesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public string Visibility { get; set; } = "public";
        public List<string>? Hashtags { get; set; }

        public static PostResponseDto FromPost(Post post, User user)
        {
            return new PostResponseDto
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                AuthorDisplayName = user.DisplayName,
                AuthorImageUrl = user.ProfileImageUrl,
                AuthorName = user.Username,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                ParentPostId = post.ParentPostId,
                LikesCount = post.LikesCount,
                RepliesCount = post.RepliesCount,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IsEdited = post.IsEdited,
                Visibility = post.Visibility,
                Hashtags = post.Hashtags
            };
        }
    }
}