using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiniX.Backend.Models
{
    public class Like
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public required string UserId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public required string PostId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
