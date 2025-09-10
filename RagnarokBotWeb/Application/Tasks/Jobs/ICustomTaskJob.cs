namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public interface ICustomTaskJob
    {
        public Task Execute(long serverId, long customTaskId);
    }
}
