namespace RagnarokBotWeb.Crosscutting.Utils
{
    public static class StringUtils
    {
        public static string RandomNumericString(int length)
        {
            Random random = new();
            const string chars = "1234567890";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
