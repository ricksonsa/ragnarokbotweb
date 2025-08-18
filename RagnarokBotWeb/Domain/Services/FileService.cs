using Microsoft.Extensions.Options;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Domain.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
        }

        public async Task<string> SaveBase64ImageAsync(string base64Image, string storagePath = "cdn-storage", string cdnUrlPrefix = "images")
        {
            var base64Parts = base64Image.Split(",");
            if (base64Parts.Length != 2)
                throw new ArgumentException("Invalid Base64 image format.");

            var header = base64Parts[0];
            var base64Data = base64Parts[1];

            var contentTypeMatch = Regex.Match(header, @"data:image/(?<type>.+);base64");
            if (!contentTypeMatch.Success)
                throw new ArgumentException("Invalid image content type.");

            var fileExtension = contentTypeMatch.Groups["type"].Value;
            var fileName = $"{Guid.NewGuid().ToString().Replace("-", string.Empty)}.{fileExtension}";
            var filePath = Path.Combine(storagePath, fileName);

            Directory.CreateDirectory(storagePath);

            var imageBytes = Convert.FromBase64String(base64Data);

            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await stream.WriteAsync(imageBytes, 0, imageBytes.Length);

            return $"{cdnUrlPrefix}/{fileName}";
        }

        public async Task<string> SaveImageStreamAsync(
         Stream imageStream,
         string contentType,
         string storagePath = "cdn-storage",
         string cdnUrlPrefix = "images")
        {
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/"))
                throw new ArgumentException("Invalid or missing image content type.", nameof(contentType));

            var fileExtension = contentType.Split('/').Last();
            var fileName = $"{Guid.NewGuid():N}.{fileExtension}";
            var filePath = Path.Combine(storagePath, fileName);

            Directory.CreateDirectory(storagePath);

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await imageStream.CopyToAsync(fileStream);

            return $"{cdnUrlPrefix}/{fileName}";
        }

        public async Task<string> SaveCompressedBase64ImageAsync(
            string base64Image,
            string storagePath = "cdn-storage",
            string cdnUrlPrefix = "images",
            int jpegQuality = 75)
        {
            // Split base64
            var base64Parts = base64Image.Split(',');
            if (base64Parts.Length != 2)
                throw new ArgumentException("Invalid Base64 image format.");

            var header = base64Parts[0];
            var base64Data = base64Parts[1];

            var match = Regex.Match(header, @"data:image/(?<type>.+);base64");
            if (!match.Success)
                throw new ArgumentException("Invalid image content type.");

            var extension = match.Groups["type"].Value.ToLower(); // "jpeg", "png", etc.
            var fileName = $"{Guid.NewGuid()}.{extension}";
            var filePath = Path.Combine(storagePath, fileName);

            // Ensure directory exists
            Directory.CreateDirectory(storagePath);

            // Decode and load image
            var imageBytes = Convert.FromBase64String(base64Data);
            await using var inputStream = new MemoryStream(imageBytes);
            using var image = await Image.LoadAsync(inputStream);

            // Optional resize (e.g., max width 1024)
            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(800, 0), Mode = ResizeMode.Max }));

            await using var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            IImageEncoder encoder = extension switch
            {
                "jpg" or "jpeg" => new JpegEncoder { Quality = jpegQuality },
                "png" => new PngEncoder { CompressionLevel = PngCompressionLevel.Level4 },
                "webp" => new WebpEncoder() { UseAlphaCompression = false },
                "gif" => new GifEncoder(),
                _ => new JpegEncoder { Quality = jpegQuality } // default fallback
            };

            await image.SaveAsync(outputStream, encoder);

            return $"{cdnUrlPrefix}/{fileName}";
        }

        public void DeleteFile(string fileName, string storagePath = "cdn-storage")
        {
            var filePath = Path.Combine(storagePath, fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
