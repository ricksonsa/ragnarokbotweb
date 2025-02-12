namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IBunkerService
    {
        Task UpdateBunkerState(string sector, bool locked, TimeSpan activation);
    }
}
