using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiniX.Backend.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string Id { get; set; }

        [BsonElement("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("bio")]
        public string? Bio { get; set; }

        [BsonElement("profileImageUrl")]
        public string? ProfileImageUrl { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("followers")]
        public int FollowersCount { get; set; } = 0;

        [BsonElement("following")]
        public int FollowingCount { get; set; } = 0;

        [BsonElement("refreshTokens")]
        public List<RefreshToken> RefreshTokens { get; set; } = [];
    }
}