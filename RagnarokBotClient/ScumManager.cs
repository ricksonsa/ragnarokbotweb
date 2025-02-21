using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RagnarokBotClient
{
    public class ScumManager
    {
        private const int Delay = 2000;
        public static CancellationToken Token;
        private const string ChatKey = "u";

        private static Task RunCommand(string command)
        {
            if (!User32.BringWindowToForeground()) { return Task.CompletedTask; }

            Task.Delay(800).Wait();
            SendKeys.SendWait(ChatKey);
            Token.ThrowIfCancellationRequested();
            Task.Delay(600).Wait();

            Task.Run(() =>
            {
                AutoResetEvent @event = new(false);
                Thread thread = new(
                    () =>
                    {
                        Clipboard.SetText(command);
                        @event.Set();
                    });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                @event.WaitOne();
            }).Wait();

            SendKeys.SendWait("^{v}");

            Token.ThrowIfCancellationRequested();

            SendKeys.SendWait("{enter}{escape}");

            Task.Delay(Delay).Wait();
            return Task.CompletedTask;
        }

        public static Task SetupBot()
        {
            if (!User32.BringWindowToForeground()) { return Task.CompletedTask; }

            SendKeys.SendWait(ChatKey);
            Token.ThrowIfCancellationRequested();
            Task.Delay(600).Wait();
            SendKeys.SendWait("{TAB}");
            Task.Delay(600).Wait();
            SendKeys.SendWait("{escape}");
            Task.Delay(Delay).Wait();
            return Task.CompletedTask;
        }

        public static Task Teleport(string steamID, string x, string y, string z)
        {
            Logger.LogWrite($"Teleporting drone to {steamID} at {x}, {y}, {z}");
            return RunCommand($"#teleport {x} {y} {z} {steamID}");
        }

        public static Task TeleportTo(string adminSteamID, string playerSteamID)
        {
            Logger.LogWrite($"Teleporting {adminSteamID} to {playerSteamID}");
            return RunCommand($"#teleportto {playerSteamID} {adminSteamID}");
        }

        public static Task TeleportBotTo(string playerSteamID)
        {
            Logger.LogWrite($"Teleporting bot to {playerSteamID}");
            return RunCommand($"#teleportto {playerSteamID}");
        }

        public static Task SpawnItem(string item, int amount)
        {
            Logger.LogWrite($"Spawning item {amount} x {item}");
            return RunCommand($"#spawnitem {item} {amount}");
        }

        public static Task SpawnItem(string item, int amount, string location)
        {
            Logger.LogWrite($"Spawning item {amount} x {item} at location {location}");
            return RunCommand($"#spawnitem {item} {amount} location {location}");
        }

        public static Task KickPlayer(string steamId)
        {
            Logger.LogWrite($"Kicking player {steamId} from the server");
            return RunCommand($"#kick {steamId}");
        }

        public static Task BanPlayer(string steamId)
        {
            Logger.LogWrite($"Banning player {steamId} from the server");
            return RunCommand($"#ban {steamId}");
        }

        public static Task Say(string text)
        {
            if (text.StartsWith("#")) text.TrimStart('#');
            Logger.LogWrite($"Bot says {text}");
            return RunCommand($"{text}");
        }

        public static Task Command(string text)
        {
            if (!text.StartsWith("#"))
            {
                Logger.LogWrite($"Invalid command {text}");
                return Task.CompletedTask;
            }
            Logger.LogWrite($"Running command {text}");
            return RunCommand($"{text}");
        }

        public static Task Announce(string content)
        {
            Logger.LogWrite($"Announcing {content}");
            return RunCommand($"#announce {content}");
        }

        public static Task DumpAllSquadsInfoList()
        {
            Logger.LogWrite($"DumpAllSquadsInfoList");
            return RunCommand("#DumpAllSquadsInfoList");
        }

        public static Task DumpListPlayers()
        {
            Logger.LogWrite($"DumpListPlayers");
            return RunCommand("#ListPlayers true");
        }

        private class User32
        {
            [DllImport("user32.dll")]
            static extern bool SetForegroundWindow(IntPtr hWnd);

            internal static bool BringWindowToForeground()
            {
                Process[] procs = Process.GetProcessesByName("SCUM");

                if (procs.Length > 0)
                {
                    if (procs[0].MainWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(procs[0].MainWindowHandle);
                        return true;
                    }
                    return false;
                }
                else
                {
                    Logger.LogWrite($"Couldnt find SCUM Window.");
                    return false;
                }
            }
        }
    }
}
