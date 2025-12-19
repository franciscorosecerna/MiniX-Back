using MongoDB.Driver;
using MiniX.Backend.Models;

namespace MiniX.Backend.Repositories
{
    public interface IPostRepository
    {
        Task<Post> CreateAsync(Post post);
        Task<Post?> GetByIdAsync(string id);
        Task<List<Post>> GetByAuthorIdAsync(string authorId, int skip = 0, int limit = 20);
        Task<List<Post>> GetRepliesAsync(string parentPostId, int skip = 0, int limit = 20);
        Task<List<Post>> GetTimelineAsync(int skip = 0, int limit = 20);
        Task<List<Post>> GetByHashtagAsync(string hashtag, int skip = 0, int limit = 20);
        Task<bool> UpdateAsync(string id, Post post);
        Task<bool> DeleteAsync(string id);
        Task<bool> IncrementLikesCountAsync(string id, int increment = 1);
        Task<bool> IncrementRepliesCountAsync(string id, int increment = 1);
        Task<long> GetCountByAuthorAsync(string authorId);
        Task CreateIndexesAsync();
        Task<string?> GetImagePublicIdByUrlAsync(string imageUrl);
    }

    public class PostRepository : IPostRepository
    {
        private readonly IMongoCollection<Post> _posts;

        public PostRepository(IMongoDatabase database)
        {
            _posts = database.GetCollection<Post>("posts");
        }

        public async Task CreateIndexesAsync()
        {
            await _posts.Indexes.CreateOneAsync(
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys.Ascending(p => p.AuthorId)
                )
            );

            await _posts.Indexes.CreateOneAsync(
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys.Descending(p => p.CreatedAt)
                )
            );

            await _posts.Indexes.CreateOneAsync(
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys.Ascending(p => p.ParentPostId)
                )
            );

            await _posts.Indexes.CreateOneAsync(
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys
                        .Ascending(p => p.AuthorId)
                        .Descending(p => p.CreatedAt)
                )
            );

            await _posts.Indexes.CreateOneAsync(
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys
                        .Ascending(p => p.ParentPostId)
                        .Descending(p => p.CreatedAt)
                )
            );

            await _posts.Indexes.CreateOneAsync(
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys.Ascending(p => p.Hashtags)
                )
            );

            await _posts.Indexes.CreateOneAsync(
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys
                        .Ascending(p => p.Visibility)
                        .Descending(p => p.CreatedAt)
                )
            );
        }

        public async Task<Post> CreateAsync(Post post)
        {
            await _posts.InsertOneAsync(post);
            return post;
        }

        public async Task<Post?> GetByIdAsync(string id)
            => await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();

        public async Task<List<Post>> GetByAuthorIdAsync(string authorId, int skip = 0, int limit = 20)
            => await _posts
                .Find(p => p.AuthorId == authorId)
                .SortByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

        public async Task<List<Post>> GetRepliesAsync(string parentPostId, int skip = 0, int limit = 20)
            => await _posts
                .Find(p => p.ParentPostId == parentPostId)
                .SortByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

        public async Task<List<Post>> GetTimelineAsync(int skip = 0, int limit = 20)
            => await _posts
                .Find(p => p.ParentPostId == null && p.Visibility == "public")
                .SortByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

        public async Task<List<Post>> GetByHashtagAsync(string hashtag, int skip = 0, int limit = 20)
            => await _posts
                .Find(Builders<Post>.Filter.AnyEq(p => p.Hashtags, hashtag))
                .SortByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

        public async Task<bool> UpdateAsync(string id, Post post)
        {
            post.UpdatedAt = DateTime.UtcNow;
            post.IsEdited = true;

            var result = await _posts.ReplaceOneAsync(p => p.Id == id, post);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _posts.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> IncrementLikesCountAsync(string id, int increment = 1)
        {
            var update = Builders<Post>.Update.Inc(p => p.LikesCount, increment);
            var result = await _posts.UpdateOneAsync(p => p.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> IncrementRepliesCountAsync(string id, int increment = 1)
        {
            var update = Builders<Post>.Update.Inc(p => p.RepliesCount, increment);
            var result = await _posts.UpdateOneAsync(p => p.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<long> GetCountByAuthorAsync(string authorId)
            => await _posts.CountDocumentsAsync(p => p.AuthorId == authorId);

        public async Task<string?> GetImagePublicIdByUrlAsync(string imageUrl)
        {
            return await _posts
                .Find(u => u.ImageUrl == imageUrl)
                .Project(u => u.ImageId)
                .FirstOrDefaultAsync();
        }
    }
}