using MiniX.Backend.Models;

namespace MiniX.Backend.DTOs
{
    public class PostResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
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

        public static PostResponseDto FromPost(Post post)
        {
            return new PostResponseDto
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
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