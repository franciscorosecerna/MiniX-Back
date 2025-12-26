using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MiniX.Backend.Repositories;

namespace MiniX.Backend.Services
{
    public interface IImageService
    {
        public Task<(string url, string id)> UploadImageAsync(IFormFile file);
        public Task<bool> DeleteImageAsync(string Url);
    }

    public class ImageService: IImageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IUserRepository _userRepository;
        private readonly IPostRepository _postRepository;

        public ImageService(Cloudinary cloudinary, IUserRepository user, IPostRepository postRepository)
        {
            _userRepository = user;
            _cloudinary = cloudinary;
            _postRepository = postRepository;
        }

        public async Task<(string url, string id)> UploadImageAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();

            using (var stream = file.OpenReadStream())
            {
                stream.Position = 0;

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "minix",
                    UseFilename = false,
                    UniqueFilename = true,
                    Overwrite = false,
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                        .Crop("scale")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return (url: uploadResult.SecureUrl.ToString(), id: uploadResult.PublicId.ToString());
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return false;

            var publicId = await _userRepository.GetImagePublicIdByUrlAsync(imageUrl);

            if (string.IsNullOrWhiteSpace(publicId))
            {
                publicId = await _postRepository.GetImagePublicIdByUrlAsync(imageUrl);

                if (string.IsNullOrWhiteSpace(publicId))
                    return false;
            }

            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));

            return result.Result == "ok";
        }
    }
}
