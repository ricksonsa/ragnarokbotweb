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
        /// Converts in-game money to Discord coins, reducing by the deposit rate.
        /// e.g., if DepositRate = 0.10, 1000 in-game money -> 900 coins
        /// </summary>
        public long ToDiscordCoins(long inGameMoney)
        {
            var rate = Server.Exchange.DepositRate;

            if (rate <= 0)
                return inGameMoney;

            double result = inGameMoney * (1 - rate);
            return (long)Math.Floor(result);
        }

        /// <summary>
        /// Converts Discord coins back to in-game money, increasing by the withdraw rate.
        /// e.g., if WithdrawRate = 0.10, 1000 coins -> 1100 in-game money
        /// </summary>
        public long ToInGameMoney(long discordCoins)
        {
            var rate = Server.Exchange.WithdrawRate;

            if (rate <= 0)
                return discordCoins;

            double result = discordCoins * (1 + rate);
            return (long)Math.Floor(result);
        }

        public long Transfer(long coins)
        {
            var rate = Server.Exchange.TransferRate;

            if (rate <= 0)
                return coins;

            double result = coins * (1 - rate);
            return (long)Math.Floor(result);
        }
    }
}
