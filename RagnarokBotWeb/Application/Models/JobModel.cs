namespace RagnarokBotWeb.Application.Models
{
    public class JobModel
    {
        public string JobID { get; set; }
        public string GroupID { get; set; }
        public DateTimeOffset? NextFireTime { get; set; }
    }
}
