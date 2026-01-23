using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiniX.Backend.Models
{
    public class Follow
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public required string Id { get; set; }

        
        public required string FollowerId { get; set; }

        
        public required string FollowingId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
