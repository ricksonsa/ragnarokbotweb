using Shared.Enums;

namespace RagnarokBotClient
{
    public class CommandHandler
    {
        private readonly CancellationToken _token;
        private readonly string _identifier;
        private readonly WebApi _remote;

        public CommandHandler(CancellationToken token, string identifier, WebApi remote)
        {
            _token = token;
            ScumManager.Token = _token;
            _remote = remote;
            _identifier = identifier;
        }

        [STAThread]
        public Task Handle(Command command)
        {
            switch (command.Type)
            {
                case ECommandType.Delivery: return ScumManager.SpawnItem(command.Value!, command.Amount, command.Target!);

                case ECommandType.Kick: return ScumManager.KickPlayer(command.Target ?? command.Value);

                case ECommandType.Ban: return ScumManager.BanPlayer(command.Target ?? command.Value);

                case ECommandType.Announce: return ScumManager.Announce(command.Value);

                case ECommandType.Say: return ScumManager.Say(command.Value);

                case ECommandType.TeleportPlayer:
                    var v = command.Coordinates!.Split(" ");
                    var x = v[0];
                    var y = v[1];
                    var z = v[2];
                    return ScumManager.Teleport(command.Target!, x, y, z);

                case ECommandType.ListPlayers: return HandleListPlayers();
            }

            return Task.CompletedTask;
        }

        private async Task HandleListPlayers()
        {
            await ScumManager.DumpListPlayers();
            await Task.Delay(1000);

            string clipboardContent = string.Empty;
            var thread = new Thread(() =>
            {
                try
                {
                    clipboardContent = Clipboard.GetText();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error accessing clipboard: " + ex.Message);
                }
            });

            thread.SetApartmentState(ApartmentState.STA); // Ensure STA for clipboard access
            thread.Start();
            thread.Join(); // Wait for the thread to complete
            var response = await _remote.GetAsync($"api/bots/players?identifier={_identifier}&input={clipboardContent}");
        }
    }
}
