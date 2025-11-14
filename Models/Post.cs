using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiniX.Backend.Models
{
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public required string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("authorId")]
        public required string AuthorId { get; set; }

        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        [BsonElement("imageUrl")]
        public string? ImageUrl { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("parentPostId")]
        public string? ParentPostId { get; set; }

        [BsonElement("likesCount")]
        public int LikesCount { get; set; } = 0;

        [BsonElement("repliesCount")]
        public int RepliesCount { get; set; } = 0;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("isEdited")]
        public bool IsEdited { get; set; } = false;

        [BsonElement("visibility")]
        public string Visibility { get; set; } = "public";

        [BsonElement("hashtags")]
        public List<string>? Hashtags { get; set; }
    }
}