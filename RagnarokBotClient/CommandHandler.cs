using Shared.Enums;

namespace RagnarokBotClient
{
    public class CommandHandler
    {
        private readonly WebApi _remote;
        private readonly ScumManager _scumManager;

        public CommandHandler(ScumManager scumManager, WebApi remote)
        {
            _remote = remote;
            _scumManager = scumManager;
        }

        [STAThread]
        public List<Func<Task>> Handle(BotCommand command)
        {
            List<Func<Task>> tasks = [];
            foreach (var commandValue in command.Values)
            {
                switch (commandValue.Type)
                {
                    case ECommandType.SimpleDelivery:
                        tasks.Add(() => _scumManager.SpawnItem(commandValue.Value!, commandValue.Amount, commandValue.Target!));
                        break;

                    case ECommandType.MagazineDelivery:
                        tasks.Add(() => _scumManager.SpawnItem(commandValue.Target!, commandValue.Amount, int.Parse(commandValue.Value), commandValue.Coordinates!));
                        break;

                    case ECommandType.Kick:
                        tasks.Add(() => _scumManager.KickPlayer(commandValue.Target ?? commandValue.Value));
                        break;

                    case ECommandType.Ban:
                        tasks.Add(() => _scumManager.BanPlayer(commandValue.Target ?? commandValue.Value));
                        break;

                    case ECommandType.Announce:
                        tasks.Add(() => _scumManager.Announce(commandValue.Value));
                        break;

                    case ECommandType.Say:
                        tasks.Add(() => _scumManager.Say(commandValue.Value));
                        break;

                    case ECommandType.SayLocal:
                        tasks.Add(() => _scumManager.SayLocal(commandValue.Value));
                        break;

                    case ECommandType.TeleportPlayer:
                        tasks.Add(() => _scumManager.Teleport(commandValue.Target!, commandValue.Coordinates!));
                        break;

                    case ECommandType.ListPlayers:
                        tasks.Add(HandleListPlayers);
                        break;

                    case ECommandType.ListSquads:
                        tasks.Add(HandleListSquads);
                        break;
                }
            }

            if (command.Values.Any(values => values.Type == ECommandType.SimpleDelivery))
            {
                tasks.Add(() => _remote.PatchAsync($"api/bots/deliveries/{command.Data.Split("_")[1]}/confirm", null));
            }


            return tasks;
        }

        private async Task HandleListPlayers()
        {
            await _scumManager.DumpListPlayers();
            await Task.Delay(1000);

            string clipboardContent = await GetClipboardTextAsync();

            var response = await _remote.PostAsync("api/bots/players", new { Value = clipboardContent });
        }

        private async Task HandleListSquads()
        {
            await _scumManager.DumpAllSquadsInfoList();
            await Task.Delay(1000);

            string clipboardContent = await GetClipboardTextAsync();

            var response = await _remote.PostAsync("api/bots/squads", new { Value = clipboardContent });
        }

        private Task<string> GetClipboardTextAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            var thread = new Thread(() =>
            {
                try
                {
                    string text = Clipboard.GetText();
                    tcs.SetResult(text);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }
    }
}
