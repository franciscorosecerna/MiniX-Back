using MongoDB.Driver;
using MiniX.Backend.Models;

namespace MiniX.Backend.Repositories
{
    public interface ILikeRepository
    {
        Task<Like> CreateAsync(Like like);
        Task<bool> DeleteByUserAndPostAsync(string userId, string postId);
        Task<bool> ExistsAsync(string userId, string postId);
        Task<List<Like>> GetByPostIdAsync(string postId, int skip = 0, int limit = 100);
        Task<long> GetCountByPostIdAsync(string postId);
        Task CreateIndexesAsync();
    }

    public class LikeRepository : ILikeRepository
    {
        private readonly IMongoCollection<Like> _likes;

        public LikeRepository(IMongoDatabase database)
        {
            _likes = database.GetCollection<Like>("likes");
        }

        public async Task CreateIndexesAsync()
        {
            await _likes.Indexes.CreateOneAsync(
                new CreateIndexModel<Like>(
                    Builders<Like>.IndexKeys
                        .Ascending(l => l.UserId)
                        .Ascending(l => l.PostId),
                    new CreateIndexOptions { Unique = true }
                )
            );

            await _likes.Indexes.CreateOneAsync(
                new CreateIndexModel<Like>(
                    Builders<Like>.IndexKeys.Ascending(l => l.PostId)
                )
            );
        }

        public async Task<Like> CreateAsync(Like like)
        {
            await _likes.InsertOneAsync(like);
            return like;
        }

        public async Task<bool> DeleteByUserAndPostAsync(string userId, string postId)
        {
            var result = await _likes.DeleteOneAsync(l => l.UserId == userId && l.PostId == postId);
            return result.DeletedCount > 0;
        }

        public async Task<bool> ExistsAsync(string userId, string postId)
        {
            var count = await _likes.CountDocumentsAsync(l => l.UserId == userId && l.PostId == postId);
            return count > 0;
        }

        public async Task<List<Like>> GetByPostIdAsync(string postId, int skip = 0, int limit = 100)
            => await _likes
                .Find(l => l.PostId == postId)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

        public async Task<long> GetCountByPostIdAsync(string postId)
            => await _likes.CountDocumentsAsync(l => l.PostId == postId);
    }
}