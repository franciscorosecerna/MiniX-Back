using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiniX.Backend.Models
{
    public class Follow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public required string FollowerId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public required string FollowingId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
