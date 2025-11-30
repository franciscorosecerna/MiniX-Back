using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using MiniX.Backend.DTOs;
using MiniX.Backend.Models;
using MiniX.Backend.Services;
using System.Security.Claims;

namespace MiniX.Backend.Controllers
{
    /// <summary>
    /// Controller for managing posts, including creation, retrieval, updates, and interactions
    /// </summary>
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IUserService _userService;
        private readonly ILogger<PostsController> _logger;

        /// <summary>
        /// Initializes a new instance of the PostsController
        /// </summary>
        /// <param name="postService">The post service for business logic operations</param>
        /// <param name="logger">The logger for logging activities</param>
        public PostsController(IPostService postService, ILogger<PostsController> logger, IUserService userService)
        {
            _postService = postService;
            _logger = logger;
            _userService = userService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;
        }

        /// <summary>
        /// Retrieves a specific post by its unique identifier
        /// </summary>
        /// <param name="id">The unique identifier of the post</param>
        /// <returns>The requested post if found</returns>
        /// <response code="200">Returns the requested post</response>
        /// <response code="404">If the post is not found</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PostResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var post = await _postService.GetPostByIdAsync(id);           
            if (post == null)
                return NotFound(new { message = "Post no encontrado" });

            var user = await _userService.GetUserByIdAsync(post.AuthorId);
            if (user == null)
                return NotFound(new { message = "Autor no encontrado" });

            var response = PostResponseDto.FromPost(post, user);
            return Ok(response);
        }

        /// <summary>
        /// Retrieves posts from a specific user with pagination support
        /// </summary>
        /// <param name="userName">The unique identifier of the user</param>
        /// <param name="page">The page number for pagination (default: 1)</param>
        /// <param name="pageSize">The number of items per page (1-100, default: 20)</param>
        /// <returns>A list of posts from the specified user</returns>
        /// <response code="200">Returns the list of user posts</response>
        [HttpGet("user/{userName}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<PostResponseDto>), 200)]
        public async Task<IActionResult> GetByUser(string userName, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            string youId = GetCurrentUserId();

            var user = await _userService.GetUserByUsernameAsync(userName);
            if(user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var posts = await _postService.GetUserPostsAsync(user.Id, page, pageSize);
            var totalCount = await _postService.GetUserPostsCountAsync(user.Id);
 
            AddPaginationHeaders(page, pageSize, totalCount);

            List<PostResponseDto> response = [];

            foreach (var post in posts)
            {
                var isLiked = await _postService.LikeExistsAsync(post.Id, youId);
                response.Add(PostResponseDto.FromPost(post, user, isLiked));
            }

            return Ok(response);
        }

        /// <summary>
        /// Retrieves replies to a specific post with pagination support
        /// </summary>
        /// <param name="id">The unique identifier of the parent post</param>
        /// <param name="page">The page number for pagination (default: 1)</param>
        /// <param name="pageSize">The number of items per page (1-100, default: 20)</param>
        /// <returns>A list of replies to the specified post</returns>
        /// <response code="200">Returns the list of post replies</response>
        [HttpGet("{id}/replies")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<PostResponseDto>), 200)]
        public async Task<IActionResult> GetReplies(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            string youId = GetCurrentUserId();

            var replies = await _postService.GetPostRepliesAsync(id, page, pageSize);

            List<PostResponseDto> response = [];

            foreach (var post in replies)
            {
                var isLiked = await _postService.LikeExistsAsync(post.Id, youId);
                var user = await _userService.GetUserByIdAsync(post.AuthorId);
                if (user == null)
                {
                    var fallbackUser = new User
                    {
                        Id = "deleted",
                        Username = "[deleted]",
                        DisplayName = "[deleted]"
                    };
                    response.Add(PostResponseDto.FromPost(post, fallbackUser, isLiked));
                }
                else response.Add(PostResponseDto.FromPost(post, user, isLiked));
            }

            return Ok(response);
        }

        /// <summary>
        /// Retrieves the timeline of posts with pagination support
        /// </summary>
        /// <param name="page">The page number for pagination (default: 1)</param>
        /// <param name="pageSize">The number of items per page (1-100, default: 20)</param>
        /// <returns>A list of posts for the timeline</returns>
        /// <response code="200">Returns the timeline posts</response>
        [HttpGet("/timeline")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<PostResponseDto>), 200)]
        public async Task<IActionResult> GetTimeline([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            string youId = GetCurrentUserId();

            var timeline = await _postService.GetTimelineAsync(page, pageSize);

            List<PostResponseDto> response = [];

            foreach (var post in timeline)
            {
                var isLiked = await _postService.LikeExistsAsync(post.Id, youId);
                var user = await _userService.GetUserByIdAsync(post.AuthorId);
                if (user == null)
                {
                    var fallbackUser = new User
                    {
                        Id = "deleted",
                        Username = "[deleted]",
                        DisplayName = "[deleted]"
                    };
                    response.Add(PostResponseDto.FromPost(post, fallbackUser, isLiked));
                }
                else response.Add(PostResponseDto.FromPost(post, user, isLiked));
            }
            return Ok(response);
        }

        /// <summary>
        /// Retrieves posts containing a specific hashtag with pagination support
        /// </summary>
        /// <param name="tag">The hashtag to search for (without the # symbol)</param>
        /// <param name="page">The page number for pagination (default: 1)</param>
        /// <param name="pageSize">The number of items per page (1-100, default: 20)</param>
        /// <returns>A list of posts containing the specified hashtag</returns>
        /// <response code="200">Returns the list of posts with the hashtag</response>
        [HttpGet("hashtag/{tag}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<PostResponseDto>), 200)]
        public async Task<IActionResult> GetByHashtag(string tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            string youId = GetCurrentUserId();

            var posts = await _postService.GetPostsByHashtagAsync(tag, page, pageSize);

            List<PostResponseDto> response = [];

            foreach (var post in posts)
            {
                var isLiked = await _postService.LikeExistsAsync(post.Id, youId);
                var user = await _userService.GetUserByIdAsync(post.AuthorId);
                if (user == null)
                {
                    var fallbackUser = new User
                    {
                        Id = "deleted",
                        Username = "[deleted]",
                        DisplayName = "[deleted]"
                    };
                    response.Add(PostResponseDto.FromPost(post, fallbackUser, isLiked));
                }
                else response.Add(PostResponseDto.FromPost(post, user, isLiked));
            }

            return Ok(response);
        }

        /// <summary>
        /// Creates a new post
        /// </summary>
        /// <param name="dto">The data transfer object containing post information</param>
        /// <returns>The newly created post</returns>
        /// <response code="201">Returns the newly created post</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PostResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string authorId = GetCurrentUserId();

            var post = await _postService.CreatePostAsync(
                authorId,
                dto.Content,
                dto.ImageUrl,
                dto.ParentPostId
            );
            var user = await _userService.GetUserByIdAsync(authorId);

            var response = PostResponseDto.FromPost(post, user!);
            return CreatedAtAction(nameof(GetById), new { id = post.Id }, response);
        }

        /// <summary>
        /// Updates an existing post
        /// </summary>
        /// <param name="id">The unique identifier of the post to update</param>
        /// <param name="dto">The data transfer object containing updated post information</param>
        /// <returns>The updated post</returns>
        /// <response code="200">Returns the updated post</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not authorized to update this post</response>
        /// <response code="404">If the post is not found</response>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(PostResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string authorId = GetCurrentUserId();

            var isLiked = await _postService.LikeExistsAsync(id, authorId);
            var updated = await _postService.UpdatePostAsync(id, authorId, dto.Content, dto.ImageUrl);

            if (updated == null)
                return NotFound(new { message = "Post no encontrado" });
            var user = await _userService.GetUserByIdAsync(authorId);

            var response = PostResponseDto.FromPost(updated, user!, isLiked);
            return Ok(response);
        }

        /// <summary>
        /// Deletes a specific post
        /// </summary>
        /// <param name="id">The unique identifier of the post to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the post was successfully deleted</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not authorized to delete this post</response>
        /// <response code="404">If the post is not found</response>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(string id)
        {
            string authorId = GetCurrentUserId();

            bool deleted = await _postService.DeletePostAsync(id, authorId);

            if (!deleted)
                return NotFound(new { message = "Post no encontrado" });

            return NoContent();
        }

        /// <summary>
        /// Adds a like to a specific post
        /// </summary>
        /// <param name="id">The unique identifier of the post to like</param>
        /// <returns>Success message</returns>
        /// <response code="200">If the like was successfully added</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the post is not found</response>
        /// <response code="409">If the user has already liked this post</response>
        [HttpPost("{id}/like")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Like(string id)
        {
            string userId = GetCurrentUserId();

            bool liked = await _postService.LikePostAsync(id, userId);

            return liked
                ? Ok(new { message = "Post likeado exitosamente" })
                : Conflict(new { message = "Ya habías dado like a este post" });
        }

        /// <summary>
        /// Removes a like from a specific post
        /// </summary>
        /// <param name="id">The unique identifier of the post to unlike</param>
        /// <returns>Success message</returns>
        /// <response code="200">If the like was successfully removed</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the post is not found or the user hadn't liked it</response>
        [HttpDelete("{id}/like")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Unlike(string id)
        {
            string userId = GetCurrentUserId();

            bool unliked = await _postService.UnlikePostAsync(id, userId);

            return unliked
                ? Ok(new { message = "Like removido exitosamente" })
                : NotFound(new { message = "No habías dado like a este post" });
        }

        private void AddPaginationHeaders(int page, int pageSize, int totalCount)
        {
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page", page.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());
            Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString());
        }
    }
}
