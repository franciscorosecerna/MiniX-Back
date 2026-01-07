using MongoDB.Driver;
using MiniX.Backend.Models;

namespace MiniX.Backend.Repositories
{
    public interface IFollowRepository
    {
        Task CreateIndexesAsync();
        Task<Follow?> GetByIdAsync(string id);
        Task<Follow?> GetFollowAsync(string followerId, string followingId);
        Task<List<Follow>> GetFollowersByUserIdAsync(string userId, int skip = 0, int limit = 20);
        Task<List<Follow>> GetFollowingByUserIdAsync(string userId, int skip = 0, int limit = 20);
        Task<bool> IsFollowingAsync(string followerId, string followingId);
        Task<(bool success, string? followId)> FollowUserAsync(string followerId, string followingId);
        Task<bool> UnfollowUserAsync(string followerId, string followingId);
        Task<int> GetFollowersCountAsync(string userId);
        Task<int> GetFollowingCountAsync(string userId);
    }

    public class FollowRepository : IFollowRepository
    {
        private readonly IMongoCollection<Follow> _follows;
        private readonly IMongoClient _mongoClient;
        private readonly IUserRepository _userRepository;

        public FollowRepository(IMongoClient mongoClient, IMongoDatabase database, IUserRepository userRepository)
        {
            _mongoClient = mongoClient;
            _follows = database.GetCollection<Follow>("follows");
            _userRepository = userRepository;
        }

        public async Task CreateIndexesAsync()
        {
            await _follows.Indexes.CreateOneAsync(
                new CreateIndexModel<Follow>(
                    Builders<Follow>.IndexKeys
                        .Ascending(f => f.FollowerId)
                        .Ascending(f => f.FollowingId),
                    new CreateIndexOptions { Unique = true }
                )
            );

            await _follows.Indexes.CreateOneAsync(
                new CreateIndexModel<Follow>(
                    Builders<Follow>.IndexKeys.Ascending(f => f.FollowerId)
                )
            );

            await _follows.Indexes.CreateOneAsync(
                new CreateIndexModel<Follow>(
                    Builders<Follow>.IndexKeys.Ascending(f => f.FollowingId)
                )
            );

            await _follows.Indexes.CreateOneAsync(
                new CreateIndexModel<Follow>(
                    Builders<Follow>.IndexKeys
                        .Ascending(f => f.FollowerId)
                        .Descending(f => f.CreatedAt)
                )
            );

            await _follows.Indexes.CreateOneAsync(
                new CreateIndexModel<Follow>(
                    Builders<Follow>.IndexKeys
                        .Ascending(f => f.FollowingId)
                        .Descending(f => f.CreatedAt)
                )
            );
        }

        public async Task<Follow?> GetByIdAsync(string id)
            => await _follows.Find(f => f.Id == id)
                .FirstOrDefaultAsync();

        public async Task<Follow?> GetFollowAsync(string followerId, string followingId)
            => await _follows.Find(f => f.FollowerId == followerId && f.FollowingId == followingId)
                .FirstOrDefaultAsync();

        public async Task<List<Follow>> GetFollowersByUserIdAsync(string userId, int skip = 0, int limit = 20)
            => await _follows.Find(f => f.FollowingId == userId)
                .SortByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

        public async Task<List<Follow>> GetFollowingByUserIdAsync(string userId, int skip = 0, int limit = 20)
            => await _follows.Find(f => f.FollowerId == userId)
                .SortByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

        public async Task<bool> IsFollowingAsync(string followerId, string followingId)
            => await _follows.Find(f => f.FollowerId == followerId && f.FollowingId == followingId)
                .Limit(1)
                .AnyAsync();

        public async Task<int> GetFollowersCountAsync(string userId)
            => (int)await _follows.CountDocumentsAsync(f => f.FollowingId == userId);

        public async Task<int> GetFollowingCountAsync(string userId)
            => (int)await _follows.CountDocumentsAsync(f => f.FollowerId == userId);

        public async Task<(bool success, string? followId)> FollowUserAsync(string followerId, string followingId)
        {
            if (followerId == followingId)
                return (false, null);

            var follower = await _userRepository.GetByIdAsync(followerId);
            var following = await _userRepository.GetByIdAsync(followingId);

            if (follower == null || following == null)
                return (false, null);

            var existingFollow = await GetFollowAsync(followerId, followingId);
            if (existingFollow != null)
                return (false, existingFollow.Id);

            var follow = new Follow
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _follows.InsertOneAsync(follow);

                await _userRepository.UpdateFollowingCountAsync(followerId, 1);
                await _userRepository.UpdateFollowersCountAsync(followingId, 1);

                return (true, follow.Id);
            }
            catch
            {
                return (false, null);
            }
        }

        public async Task<bool> UnfollowUserAsync(string followerId, string followingId)
        {
            var deleteResult = await _follows.DeleteOneAsync(
                f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (deleteResult.DeletedCount == 0)
                return false;

            try
            {
                await _userRepository.UpdateFollowingCountAsync(followerId, -1);
                await _userRepository.UpdateFollowersCountAsync(followingId, -1);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
