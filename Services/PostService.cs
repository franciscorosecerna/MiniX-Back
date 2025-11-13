using MiniX.Backend.Models;
using MiniX.Backend.Repositories;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace MiniX.Backend.Services
{
    public interface IPostService
    {
        Task<Post> CreatePostAsync(string authorId, string content, string? imageUrl = null, string? parentPostId = null);
        Task<Post?> GetPostByIdAsync(string id);
        Task<List<Post>> GetUserPostsAsync(string authorId, int page = 1, int pageSize = 20);
        Task<List<Post>> GetPostRepliesAsync(string postId, int page = 1, int pageSize = 20);
        Task<List<Post>> GetTimelineAsync(int page = 1, int pageSize = 20);
        Task<List<Post>> GetPostsByHashtagAsync(string hashtag, int page = 1, int pageSize = 20);
        Task<Post?> UpdatePostAsync(string id, string authorId, string content, string? imageUrl = null);
        Task<bool> DeletePostAsync(string id, string authorId);
        Task<bool> LikePostAsync(string postId, string userId);
        Task<bool> UnlikePostAsync(string postId, string userId);
        Task<int> GetUserPostsCountAsync(string authorId);
    }

    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly ILikeRepository _likeRepository;

        public PostService(IPostRepository postRepository, ILikeRepository likeRepository)
        {
            _postRepository = postRepository;
            _likeRepository = likeRepository;
        }

        public async Task<Post> CreatePostAsync(string authorId, string content, string? imageUrl = null, string? parentPostId = null)
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new ArgumentException("El post debe contener texto o imagen");
            }

            if (content?.Length > 280)
            {
                throw new ArgumentException("El contenido no puede exceder 280 caracteres");
            }

            var hashtags = ExtractHashtags(content);

            var post = new Post
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                AuthorId = authorId,
                Content = content ?? string.Empty,
                ImageUrl = imageUrl,
                ParentPostId = parentPostId,
                Hashtags = hashtags.Count != 0 ? hashtags : null,
                CreatedAt = DateTime.UtcNow
            };

            var createdPost = await _postRepository.CreateAsync(post);

            if (!string.IsNullOrEmpty(parentPostId))
            {
                await _postRepository.IncrementRepliesCountAsync(parentPostId, 1);
            }

            return createdPost;
        }

        public async Task<Post?> GetPostByIdAsync(string id)
            => await _postRepository.GetByIdAsync(id);

        public async Task<List<Post>> GetUserPostsAsync(string authorId, int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            return await _postRepository.GetByAuthorIdAsync(authorId, skip, pageSize);
        }

        public async Task<List<Post>> GetPostRepliesAsync(string postId, int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            return await _postRepository.GetRepliesAsync(postId, skip, pageSize);
        }

        public async Task<List<Post>> GetTimelineAsync(int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            return await _postRepository.GetTimelineAsync(skip, pageSize);
        }

        public async Task<List<Post>> GetPostsByHashtagAsync(string hashtag, int page = 1, int pageSize = 20)
        {
            hashtag = hashtag.TrimStart('#').ToLower();

            var skip = (page - 1) * pageSize;
            return await _postRepository.GetByHashtagAsync(hashtag, skip, pageSize);
        }

        public async Task<Post?> UpdatePostAsync(string id, string authorId, string content, string? imageUrl = null)
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new ArgumentException("El post debe contener texto o imagen");
            }

            if (content?.Length > 280)
            {
                throw new ArgumentException("El contenido no puede exceder 280 caracteres");
            }

            var existingPost = await _postRepository.GetByIdAsync(id);

            if (existingPost == null)
            {
                return null;
            }

            if (existingPost.AuthorId != authorId)
            {
                throw new UnauthorizedAccessException("No tienes permiso para editar este post");
            }

            existingPost.Content = content ?? string.Empty;
            existingPost.ImageUrl = imageUrl;
            existingPost.Hashtags = ExtractHashtags(content);
            existingPost.UpdatedAt = DateTime.UtcNow;
            existingPost.IsEdited = true;

            var updated = await _postRepository.UpdateAsync(id, existingPost);

            return updated ? existingPost : null;
        }

        public async Task<bool> DeletePostAsync(string id, string authorId)
        {
            var post = await _postRepository.GetByIdAsync(id);

            if (post == null)
            {
                return false;
            }

            if (post.AuthorId != authorId)
            {
                throw new UnauthorizedAccessException("No tienes permiso para eliminar este post");
            }

            if (!string.IsNullOrEmpty(post.ParentPostId))
            {
                await _postRepository.IncrementRepliesCountAsync(post.ParentPostId, -1);
            }

            return await _postRepository.DeleteAsync(id);
        }

        public async Task<bool> LikePostAsync(string postId, string userId)
        {
            _ = await _postRepository.GetByIdAsync(postId) 
                ?? throw new ArgumentException("El post no existe");

            var like = new Like
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                UserId = userId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _likeRepository.CreateAsync(like);

                await _postRepository.IncrementLikesCountAsync(postId, 1);

                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        public async Task<bool> UnlikePostAsync(string postId, string userId)
        {
            var deleted = await _likeRepository.DeleteByUserAndPostAsync(userId, postId);

            if (deleted)
            {
                await _postRepository.IncrementLikesCountAsync(postId, -1);
            }

            return deleted;
        }

        public async Task<int> GetUserPostsCountAsync(string authorId)
        {
            return (int)await _postRepository.GetCountByAuthorAsync(authorId);
        }

        private static List<string> ExtractHashtags(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return [];
            }

            var regex = new Regex(@"#([\p{L}\p{N}_\-]+)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(content);

            return [.. matches
                .Select(m => m.Groups[1].Value.ToLower())
                .Distinct()];
        }
    }
}