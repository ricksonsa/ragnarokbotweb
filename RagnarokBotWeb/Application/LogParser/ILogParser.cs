namespace RagnarokBotWeb.Application.LogParser
{
    public interface ILogParser<T>
    {
        T Parse(string line);
    }
}
