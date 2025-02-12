using RagnarokBotWeb.Crosscutting.Utils;

namespace RagnarokBotWeb.Application.Models
{
    public class Line
    {
        public string Value { get; set; }
        public string Hash { get; set; }
        public string File { get; set; }

        public Line(string value, string file)
        {
            Value = value;
            File = file;
            Hash = HashUtil.ComputeSHA256Hash(value);
        }

        public Line(string value, string file, string hash)
        {
            Value = value;
            File = file;
            Hash = hash;
        }
    }
}
