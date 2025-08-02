namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IFileService
    {
        Task<string> SaveBase64ImageAsync(string base64Image, string storagePath = "cdn-storage", string cdnUrlPrefix = "images");
        Task<string> SaveCompressedBase64ImageAsync(
            string base64Image,
            string storagePath = "cdn-storage",
            string cdnUrlPrefix = "images",
            int jpegQuality = 75);

        Task<string> SaveImageStreamAsync(
        Stream imageStream,
        string contentType,
        string storagePath = "cdn-storage",
        string cdnUrlPrefix = "images");

        void DeleteFile(string fileName, string storagePath = "cdn-storage");
    }
}
