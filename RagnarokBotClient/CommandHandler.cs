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
        public List<Func<Task>> Handle(BotCommand command)
        {
            List<Func<Task>> tasks = [];
            foreach (var commandValue in command.Values)
            {
                switch (commandValue.Type)
                {
                    case ECommandType.Delivery:
                        tasks.Add(() => ScumManager.SpawnItem(commandValue.Value!, commandValue.Amount, commandValue.Target!));
                        break;

                    case ECommandType.Kick:
                        tasks.Add(() => ScumManager.KickPlayer(commandValue.Target ?? commandValue.Value));
                        break;

                    case ECommandType.Ban:
                        tasks.Add(() => ScumManager.BanPlayer(commandValue.Target ?? commandValue.Value));
                        break;

                    case ECommandType.Announce:
                        tasks.Add(() => ScumManager.Announce(commandValue.Value));
                        break;

                    case ECommandType.Say:
                        tasks.Add(() => ScumManager.Say(commandValue.Value));
                        break;

                    case ECommandType.TeleportPlayer:
                        tasks.Add(() => ScumManager.Teleport(commandValue.Target!, commandValue.Coordinates!));
                        break;

                    case ECommandType.ListPlayers:
                        tasks.Add(HandleListPlayers);
                        break;
                }
            }

            if (command.Values.Any(values => values.Type == ECommandType.Delivery))
            {
                tasks.Add(() => _remote.PatchAsync($"api/bots/deliveries/{command.Data.Split("_")[1]}/confirm", null));
            }


            return tasks;
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
            var response = await _remote.PostAsync($"api/bots/players", new { Value = clipboardContent });
        }
    }
}
