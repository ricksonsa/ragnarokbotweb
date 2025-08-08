namespace RagnarokBotWeb.Crosscutting.Utils
{

    public static class ColorUtil
    {
        private static readonly Random _random = new Random();

        public static string GetRandomColor()
        {
            // Generate RGB values from 0 to 255
            int r = _random.Next(256);
            int g = _random.Next(256);
            int b = _random.Next(256);

            // Return as hex string (e.g., "#A1B2C3")
            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}
