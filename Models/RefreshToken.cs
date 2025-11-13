using MongoDB.Bson.Serialization.Attributes;

namespace MiniX.Backend.Models { 
    public class RefreshToken
    {
        [BsonElement("token")]
        public string Token { get; set; } = string.Empty;

        [BsonElement("expires")]
        public DateTime Expires { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("revokedAt")]
        public DateTime? RevokedAt { get; set; }
    }
}
