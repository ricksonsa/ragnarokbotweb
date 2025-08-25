using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Application.Handlers
{
    public class CoinConverterManager
    {
        public ScumServer Server { get; }

        public CoinConverterManager(ScumServer server)
        {
            Server = server;
        }

        /// <summary>
        /// Converts in-game money to Discord coins.
        /// </summary>
        /// <param name="inGameMoney">The amount of in-game money.</param>
        /// <param name="exchangeRate">
        /// The exchange rate (how many Discord coins you get for 1 in-game money).
        /// </param>
        /// <returns>The equivalent amount of Discord coins.</returns>
        public long ToDiscordCoins(long inGameMoney)
        {
            var exchangeRate = Server.Exchange.DepositRate;
            if (exchangeRate <= 0)
                return inGameMoney;

            double result = inGameMoney * (1 - exchangeRate);
            return (long)Math.Floor(result);
        }

        /// <summary>
        /// Converts Discord coins back to in-game money.
        /// </summary>
        /// <param name="discordCoins">The amount of Discord coins.</param>
        /// <param name="exchangeRate">
        /// The exchange rate (how many Discord coins you get for 1 in-game money).
        /// </param>
        /// <returns>The equivalent amount of in-game money.</returns>
        public long ToInGameMoney(long discordCoins)
        {
            var exchangeRate = Server.Exchange.WithdrawRate;

            if (exchangeRate <= 0)
                return discordCoins;

            return (long)Math.Floor(discordCoins / (1 - exchangeRate));
        }
    }
}
