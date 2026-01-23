using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Reflection.Metadata;

namespace MiniX.Backend.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public required string Id { get; set; }

        [BsonElement("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string? PasswordHash { get; set; } = string.Empty;

        [BsonElement("bio")]
        public string? Bio { get; set; }

        [BsonElement("profileImageUrl")]
        public string? ProfileImageUrl { get; set; }

        [BsonElement("profileImageId")]
        public string? ProfileImageId { get; set; } = "";

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("followers")]
        public int FollowersCount { get; set; } = 0;

        [BsonElement("following")]
        public int FollowingCount { get; set; } = 0;

        [BsonElement("role")]
        public string Role { get; set; } = "User";

        [BsonElement("providerAuth")]
        public string AuthProvider { get; set; } = "local"; // local | google

        [BsonElement("otp")]
        public string? Otp { get; set; }

        [BsonElement("refreshTokens")]
        public List<RefreshToken> RefreshTokens { get; set; } = [];

    }

}
