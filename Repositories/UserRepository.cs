using MiniX.Backend.Models;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace MiniX.Backend.Repositories
{
    public interface IUserRepository
    {
        Task CreateIndexesAsync();
        Task<User?> GetByIdAsync(string id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<(List<User> users, bool hasMore)> GetAllAsync(int skip = 0, int limit = 20, string q = "");
        Task<User> CreateAsync(User user);
        Task<bool> UpdateAsync(string id, UpdateDefinition<User> update);
        Task<bool> DeleteAsync(string id);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UpdateFollowersCountAsync(string userId, int increment, IClientSessionHandle? session = null);
        Task<bool> UpdateFollowingCountAsync(string userId, int increment, IClientSessionHandle? session = null);
        Task<List<User>> SearchByUsernameAsync(string searchTerm, int limit = 10);
        Task<List<User>> GetUsersByIdsAsync(IEnumerable<string> ids);
        Task<bool> AddRefreshTokenAsync(string userId, RefreshToken token);
        Task<bool> RevokeRefreshTokenAsync(string userId, string token);
        Task<bool> ReplaceRefreshTokensAsync(string userId, List<RefreshToken> tokens);
        Task<User?> GetByRefreshTokenAsync(string refreshToken);
        Task RemoveExpiredRefreshTokensAsync(string userId);
        Task<string?> GetImagePublicIdByUrlAsync(string imageUrl);
        Task<bool> ToggleAdmin(bool isAdmin, string userId);
    }

    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("users");
        }

        public async Task<bool> ToggleAdmin(bool isAdmin, string userId){
            var update = Builders<User>.Update.Set(u => u.Role, isAdmin ? "User" : "Admin");
            var result = await _users.UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<User?> GetByIdAsync(string id)
            => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task<User?> GetByUsernameAsync(string username)
            => await _users.Find(u => u.Username == username)
                .FirstOrDefaultAsync();

        public async Task<User?> GetByEmailAsync(string email)
            => await _users.Find(u => u.Email == email)
                .FirstOrDefaultAsync();

        public async Task<(List<User>users, bool hasMore)> GetAllAsync(int skip = 0, int limit = 20, string q = "") {
            var filter = string.IsNullOrWhiteSpace(q)
                ? Builders<User>.Filter.Empty
                : Builders<User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(q, "i"));

            var users = await _users.Find(filter)
                .Skip(skip)
                .Limit(limit + 1)
                .ToListAsync();

            var hasMore = users.Count > limit;
            if (hasMore)
            {
                users.RemoveAt(users.Count - 1);
            }

            return (users, hasMore);
        }

        public async Task<User> CreateAsync(User user)
        {
            await _users.InsertOneAsync(user);
            return user;
        }

        public async Task<bool> UpdateAsync(string id, UpdateDefinition<User> update)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(null, nameof(id));
            ArgumentNullException.ThrowIfNull(update);

            var result = await _users.UpdateOneAsync(u => u.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _users.DeleteOneAsync(u => u.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UsernameExistsAsync(string username)
            => await _users.Find(u => u.Username == username).AnyAsync();


        public async Task<bool> EmailExistsAsync(string email)
        {
            var count = await _users.CountDocumentsAsync(u => u.Email == email);
            return count > 0;
        }

        public async Task<bool> UpdateFollowersCountAsync(string userId, int increment, IClientSessionHandle? session = null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Inc(u => u.FollowersCount, increment);

            var result = session != null
                ? await _users.UpdateOneAsync(session, filter, update)
                : await _users.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateFollowingCountAsync(string userId, int increment, IClientSessionHandle? session = null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Inc(u => u.FollowingCount, increment);

            var result = session != null
                ? await _users.UpdateOneAsync(session, filter, update)
                : await _users.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<List<User>> SearchByUsernameAsync(string searchTerm, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            var pattern = $"^{Regex.Escape(searchTerm)}";
            var filter = Builders<User>.Filter.Regex(u => u.Username,
                new MongoDB.Bson.BsonRegularExpression(pattern, "i"));
            return await _users.Find(filter).Limit(limit).ToListAsync();
        }

        public async Task<List<User>> GetUsersByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
            if (idList.Count == 0)
                return [];

            var filter = Builders<User>.Filter.In(u => u.Id, idList);
            return await _users.Find(filter).ToListAsync();
        }

        public async Task CreateIndexesAsync()
        {
            var emailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var emailIndexOptions = new CreateIndexOptions { Unique = true };
            await _users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    emailIndexKeys, emailIndexOptions
                    )
                );

            var usernameIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Username);
            var usernameIndexOptions = new CreateIndexOptions { Unique = true };
            await _users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    usernameIndexKeys, usernameIndexOptions
                    )
                );

            await _users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Text(u => u.Username)
                )
            );
        }

        public async Task<bool> AddRefreshTokenAsync(string userId, RefreshToken token)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Push(u => u.RefreshTokens, token);

            var result = await _users.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string userId, string token)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.ElemMatch(u => u.RefreshTokens, rt => rt.Token == token)
            );

            var update = Builders<User>.Update.Set("refreshTokens.$.revokedAt", DateTime.UtcNow);

            var result = await _users.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> ReplaceRefreshTokensAsync(string userId, List<RefreshToken> tokens)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.RefreshTokens, tokens);

            var result = await _users.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            var filter = Builders<User>.Filter.ElemMatch(
                u => u.RefreshTokens,
                rt => rt.Token == refreshToken &&
                      rt.Expires > DateTime.UtcNow &&
                      rt.RevokedAt == null
            );

            return await _users.Find(filter).FirstOrDefaultAsync();
        }

        public async Task RemoveExpiredRefreshTokensAsync(string userId)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.ElemMatch(
                    u => u.RefreshTokens,
                    rt => rt.Expires < DateTime.UtcNow
                )
            );

            var update = Builders<User>.Update.PullFilter(
                u => u.RefreshTokens,
                rt => rt.Expires < DateTime.UtcNow
            );

            await _users.UpdateOneAsync(filter, update);
        }

        public async Task<string?> GetImagePublicIdByUrlAsync(string imageUrl)
        {
            return await _users
                .Find(u => u.ProfileImageUrl == imageUrl)
                .Project(u => u.ProfileImageId)
                .FirstOrDefaultAsync();
        }
    }
}
