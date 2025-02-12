namespace RagnarokBotWeb.Domain.Entities
{
    public class Reader : BaseEntity
    {
        public string FileName { get; set; }
        public string Value { get; set; }
        public string Hash { get; set; }
        public DateTime CreateDate { get; set; }

        public Reader(string fileName, string value, string hash)
        {
            FileName = fileName;
            Value = value;
            Hash = hash;
            CreateDate = DateTime.Now;
        }
    }
}
