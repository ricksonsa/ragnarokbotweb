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

        public static IEnumerable<string> ToLines(this string s)
        {
            using var reader = new StringReader(s);
            string? line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }
    }
}
