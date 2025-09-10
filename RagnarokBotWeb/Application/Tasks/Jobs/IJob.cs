namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public interface IJob
    {
        public Task Execute(long serverId);
    }
}
