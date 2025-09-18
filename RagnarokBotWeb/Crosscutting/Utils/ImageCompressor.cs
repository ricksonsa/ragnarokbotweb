using System.Drawing;
using System.Drawing.Imaging;

namespace RagnarokBotWeb.Crosscutting.Utils
{
    public static class ImageCompressor
    {
        public static string CompressAndResizeBase64Image(string base64Image, int maxLength = 2048)
        {
            const string prefix = "data:image/jpeg;base64,";
            maxLength -= prefix.Length;

            string cleaned = CleanBase64(base64Image);
            byte[] imageBytes;

            try
            {
                imageBytes = Convert.FromBase64String(cleaned);
            }
            catch
            {
                throw new ArgumentException("Input is not a valid Base64 string.");
            }

            using var inputStream = new MemoryStream(imageBytes);
            using var originalImage = Image.FromStream(inputStream);

            var jpegCodec = GetEncoder(ImageFormat.Jpeg) ?? throw new Exception("JPEG codec not found.");

            int width = originalImage.Width;
            int height = originalImage.Height;

            long quality = 90L;
            string resultBase64 = null;

            while (width > 10 && height > 10)
            {
                using var resizedImage = new Bitmap(originalImage, new Size(width, height));

                quality = 90;
                while (quality >= 5)
                {
                    using var outputStream = new MemoryStream();
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                    resizedImage.Save(outputStream, jpegCodec, encoderParams);

                    var bytes = outputStream.ToArray();
                    resultBase64 = Convert.ToBase64String(bytes);

                    if (resultBase64.Length <= maxLength)
                        return prefix + resultBase64;

                    quality -= 5;
                }

                // Reduce dimensions by 10% and try again
                width = (int)(width * 0.9);
                height = (int)(height * 0.9);
            }

            throw new Exception("Could not compress and resize image to <= 2048 characters.");
        }

        private static string CleanBase64(string base64)
        {
            if (base64.Contains(","))
                base64 = base64.Substring(base64.IndexOf(',') + 1);

            return base64.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }

}
