using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace MiniX.Backend.Services
{
    public interface IImageService
    {
        public Task<string> UploadImageAsync(IFormFile file);
        public Task<bool> DeleteImageAsync(string imageUrl);
    }

    public class ImageService: IImageService
    {
        private readonly Cloudinary _cloudinary;

        public ImageService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();

            using (var stream = file.OpenReadStream())
            {
                // esto es por las dudas que el stream no se inicialize en 0
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

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return false;

            string? publicId = ExtractPublicId(imageUrl);

            if (publicId == null)
                return false;

            var deletionParams = new DeletionParams(publicId);

            var result = await _cloudinary.DestroyAsync(deletionParams);

            return result.Result == "ok" || result.Result == "not found";
        }

        private static string? ExtractPublicId(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);

                var path = uri.AbsolutePath;

                var parts = path.Split("/image/upload/");

                if (parts.Length < 2)
                    return null;

                var publicPart = parts[1];

                var withoutExtension = publicPart[..publicPart.LastIndexOf('.')
                ];
                return withoutExtension;
            }
            catch
            {
                return null;
            }
        }
    }
}
