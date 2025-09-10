namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public interface IWarzoneJob
    {
        public Task Execute(long serverId, long warzoneId);
    }
}
