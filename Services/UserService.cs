using Microsoft.AspNetCore.Mvc;
using MiniX.Backend.DTOs;
using MiniX.Backend.Models;
using MiniX.Backend.Repositories;
using MongoDB.Driver;

namespace MiniX.Backend.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(string id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<List<User>> GetUsersAsync(int skip = 0, int limit = 20);
        Task<bool> UpdateUserAsync(string id, UpdateUserRequest update);
        Task<bool> ChangePasswordAsync(string id, string currentPlainPassword, string newPlainPassword);
        Task<bool> PasswordResetAsync(string id, string newPlainPassword);
        Task<bool> DeleteUserAsync(string id);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email);
        Task<(bool success, string? followId)> FollowUserAsync(string followerId, string followingId);
        Task<bool> UnfollowUserAsync(string followerId, string followingId);
        Task<bool> IsFollowingAsync(string followerId, string followingId);
        Task<List<User>> GetFollowersAsync(string userId, int skip = 0, int limit = 20);
        Task<List<User>> GetFollowingAsync(string userId, int skip = 0, int limit = 20);
        Task<int> GetFollowersCountAsync(string userId);
        Task<int> GetFollowingCountAsync(string userId);
        Task<List<User>> SearchUsersAsync(string searchTerm, int limit = 10);
        Task<bool> ValidateUserAsync(string userId);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IFollowRepository _followRepository;
        private readonly IImageService _imageService;

        public UserService(IUserRepository userRepository, 
            IFollowRepository followRepository, 
            IImageService imageService)
        {
            _userRepository = userRepository;
            _followRepository = followRepository;
            _imageService = imageService;
        }

        private static string NormalizeUsername(string username)
            => username?.Trim().ToLowerInvariant() ?? string.Empty;

        public async Task<User?> GetUserByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("El ID no puede estar vacío", nameof(id));
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("El nombre de usuario no puede estar vacío", nameof(username));
            return await _userRepository.GetByUsernameAsync(NormalizeUsername(username));
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("El email no puede estar vacío", nameof(email));
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<List<User>> GetUsersAsync(int skip = 0, int limit = 20)
        {
            skip = (skip - 1) * limit;
            return await _userRepository.GetAllAsync(skip, limit);
        }

        public async Task<bool> UpdateUserAsync(string id, UpdateUserRequest user)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El ID no puede estar vacío", nameof(id));

            ArgumentNullException.ThrowIfNull(user);

            var existing = await _userRepository.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"Usuario con ID '{id}' no encontrado");

            var newUsername = NormalizeUsername(user.Username ?? existing.Username);

            if (!string.Equals(existing.Username, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userRepository.UsernameExistsAsync(newUsername))
                    throw new InvalidOperationException($"El nombre de usuario '{newUsername}' ya está en uso");
            }

            if (!string.Equals(existing.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userRepository.EmailExistsAsync(user.Email!))
                    throw new InvalidOperationException($"El email '{user.Email}' ya está registrado");
            }

            string? finalImageUrl = existing.ProfileImageUrl;

            if (user.ProfileImage != null)
            {
                finalImageUrl = await _imageService.UploadImageAsync(user.ProfileImage);

                if (!string.IsNullOrEmpty(existing.ProfileImageUrl))
                {
                    try
                    {
                        await _imageService.DeleteImageAsync(existing.ProfileImageUrl);
                    }
                    finally { }
                }
            }

            var updates = new List<UpdateDefinition<User>>();
            var builder = Builders<User>.Update;

            if (!string.IsNullOrWhiteSpace(user.DisplayName) && user.DisplayName != existing.DisplayName)
                updates.Add(builder.Set(u => u.DisplayName, user.DisplayName));

            if (!string.IsNullOrWhiteSpace(user.Username) && newUsername != existing.Username)
                updates.Add(builder.Set(u => u.Username, newUsername));

            if (!string.IsNullOrWhiteSpace(user.Bio) && user.Bio != existing.Bio)
                updates.Add(builder.Set(u => u.Bio, user.Bio));

            if (!string.IsNullOrEmpty(finalImageUrl) && finalImageUrl != existing.ProfileImageUrl)
                updates.Add(builder.Set(u => u.ProfileImageUrl, finalImageUrl));

            if (!string.IsNullOrWhiteSpace(user.Email) && !string.Equals(user.Email, existing.Email, StringComparison.OrdinalIgnoreCase))
                updates.Add(builder.Set(u => u.Email, user.Email));

            if (updates.Count == 0)
                return false;

            var combined = builder.Combine(updates);

            return await _userRepository.UpdateAsync(id, combined);
        }


        public async Task<bool> ChangePasswordAsync(string id, string currentPlainPassword, string newPlainPassword)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(null, nameof(id));
            if (string.IsNullOrWhiteSpace(currentPlainPassword)) throw new ArgumentException(null, nameof(currentPlainPassword));
            if (string.IsNullOrWhiteSpace(newPlainPassword)) throw new ArgumentException(null, nameof(newPlainPassword));

            var user = await _userRepository.GetByIdAsync(id) 
                ?? throw new InvalidOperationException("Usuario no encontrado");
            if (!BCrypt.Net.BCrypt.Verify(currentPlainPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Password actual incorrecto");

            var newHash = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);
            var update = Builders<User>.Update.Set(u => u.PasswordHash, newHash);
            return await _userRepository.UpdateAsync(id, update);
        }

        public async Task<bool> PasswordResetAsync(string id, string newPlainPassword){
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(null, nameof(id));
            if (string.IsNullOrWhiteSpace(newPlainPassword)) throw new ArgumentException(null, nameof(newPlainPassword));

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new InvalidOperationException("Usuario no encontrado");

            var newHash = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);
            var update = Builders<User>.Update.Set(u => u.PasswordHash, newHash);
            return await _userRepository.UpdateAsync(id, update);
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("El ID no puede estar vacío", nameof(id));
            return await _userRepository.DeleteAsync(id);
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException(null, nameof(username));
            username = NormalizeUsername(username);
            return !await _userRepository.UsernameExistsAsync(username);
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException(null, nameof(email));
            return !await _userRepository.EmailExistsAsync(email);
        }

        public async Task<(bool success, string? followId)> FollowUserAsync(string followerId, string followingId)
        {
            if (string.IsNullOrWhiteSpace(followerId)) throw new ArgumentException(null, nameof(followerId));
            if (string.IsNullOrWhiteSpace(followingId)) throw new ArgumentException(null, nameof(followingId));
            if (followerId == followingId) throw new InvalidOperationException("Un usuario no puede seguirse a sí mismo");

            var result = await _followRepository.FollowUserAsync(followerId, followingId);

            if (!result.success && result.followId != null)
                throw new InvalidOperationException("Ya estás siguiendo a este usuario");

            if (!result.success)
                throw new InvalidOperationException("No se pudo completar la operación de seguir al usuario");

            return result;
        }

        public async Task<bool> UnfollowUserAsync(string followerId, string followingId)
        {
            if (string.IsNullOrWhiteSpace(followerId)) throw new ArgumentException(null, nameof(followerId));
            if (string.IsNullOrWhiteSpace(followingId)) throw new ArgumentException(null, nameof(followingId));
            if (followerId == followingId) throw new InvalidOperationException("Un usuario no puede dejar de seguirse a sí mismo");

            var result = await _followRepository.UnfollowUserAsync(followerId, followingId);
            if (!result) throw new InvalidOperationException("No estás siguiendo a este usuario o la operación falló");
            return result;
        }

        public async Task<bool> IsFollowingAsync(string followerId, string followingId)
        {
            if (string.IsNullOrWhiteSpace(followerId) || string.IsNullOrWhiteSpace(followingId)) return false;
            return await _followRepository.IsFollowingAsync(followerId, followingId);
        }

        public async Task<List<User>> GetFollowersAsync(string userId, int skip = 0, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(null, nameof(userId));
            if (skip < 0) throw new ArgumentException(null, nameof(skip));
            if (limit <= 0 || limit > 100) throw new ArgumentException(null, nameof(limit));

            var follows = await _followRepository.GetFollowersByUserIdAsync(userId, skip, limit);
            var followerIds = follows.Select(f => f.FollowerId).ToList();
            if (followerIds.Count == 0) return [];

            var users = await _userRepository.GetUsersByIdsAsync(followerIds);
            var usersById = users.ToDictionary(u => u.Id, u => u);
            return followerIds.Where(id => usersById.ContainsKey(id)).Select(id => usersById[id]).ToList();
        }

        public async Task<List<User>> GetFollowingAsync(string userId, int skip = 0, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(null, nameof(userId));
            if (skip < 0) throw new ArgumentException(null, nameof(skip));
            if (limit <= 0 || limit > 100) throw new ArgumentException(null, nameof(limit));

            var follows = await _followRepository.GetFollowingByUserIdAsync(userId, skip, limit);
            var followingIds = follows.Select(f => f.FollowingId).ToList();
            if (followingIds.Count == 0) return [];

            var users = await _userRepository.GetUsersByIdsAsync(followingIds);
            var usersById = users.ToDictionary(u => u.Id, u => u);
            return followingIds.Where(id => usersById.ContainsKey(id)).Select(id => usersById[id]).ToList();
        }

        public async Task<int> GetFollowersCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(null, nameof(userId));
            return await _followRepository.GetFollowersCountAsync(userId);
        }

        public async Task<int> GetFollowingCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(null, nameof(userId));
            return await _followRepository.GetFollowingCountAsync(userId);
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return [];
            if (limit <= 0) throw new ArgumentException(null, nameof(limit));
            return await _userRepository.SearchByUsernameAsync(searchTerm, limit);
        }

        public async Task<bool> ValidateUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null;
        }
    }
}
