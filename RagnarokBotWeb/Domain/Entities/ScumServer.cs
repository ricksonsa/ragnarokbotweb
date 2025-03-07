
namespace RagnarokBotWeb.Domain.Entities
{
    public class ScumServer : BaseEntity
    {
        public string? Name { get; set; }
        public Tenant Tenant { get; set; }
        public Guild? Guild { get; set; }
        public Ftp? Ftp { get; set; }
        public string? RestartTimes { get; private set; }

        public ScumServer(Tenant tenant)
        {
            Tenant = tenant;
        }

        public ScumServer() { }

        public void SetRestartTimes(List<string> restartTimes)
        {
            RestartTimes = string.Join(";", restartTimes);
        }

        public List<string> GetRestartTimesList()
        {
            return string.IsNullOrEmpty(RestartTimes) ? new List<string>() : RestartTimes.Split(";").ToList();
        }
    }
}
