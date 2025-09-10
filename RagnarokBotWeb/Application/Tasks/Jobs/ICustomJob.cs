namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public interface ICustomJob
    {
        public Task Execute(long serverId, string commands);
    }
}
