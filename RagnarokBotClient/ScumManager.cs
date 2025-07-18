using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RagnarokBotClient
{
    public class ScumManager
    {
        private const int Delay = 2000;
        public static CancellationToken Token;
        private const string ChatKey = "u";

        private static Task Sleep(int ms)
        {
            return Task.Delay(ms);
        }

        private static Task RunCommand(string command, int tab = 0)
        {
            if (!User32.BringWindowToForeground()) { return Task.CompletedTask; }

            Sleep(1000).Wait();

            SendKeys.SendWait(ChatKey);
            Token.ThrowIfCancellationRequested();

            Sleep(1000).Wait();

            switch (tab)
            {
                case 0:
                default:
                    break;
                case 1:
                    SendKeys.SendWait("{tab}");
                    Sleep(1000).Wait();
                    break;
            }

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
            Sleep(1000).Wait();

            Token.ThrowIfCancellationRequested();

            SendKeys.SendWait("{enter}");
            Sleep(1000).Wait();

            switch (tab)
            {
                case 0:
                default:
                    break;
                case 1:
                    SendKeys.SendWait("{tab}");
                    Sleep(1000).Wait();
                    SendKeys.SendWait("{tab}");
                    break;
            }
            Sleep(1000).Wait();

            SendKeys.SendWait("{escape}");

            Sleep(3000).Wait();
            return Task.CompletedTask;
        }

        public static Task Start()
        {
            if (!User32.ReconnectToServer()) { return Task.CompletedTask; }

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
            Logger.LogWrite($"Teleporting {steamID} to {x}, {y}, {z}");
            return RunCommand($"#teleport {x} {y} {z} {steamID}");
        }

        public static Task Teleport(string steamID, string coordinates)
        {
            Logger.LogWrite($"Teleporting {steamID} to {coordinates}");
            return RunCommand($"#teleport {coordinates} {steamID}");
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
            return RunCommand($"{text}", tab: 1);
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
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            [DllImport("user32.dll")]
            static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

            [DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            // Mouse

            [DllImport("user32.dll")]
            static extern bool SetCursorPos(int X, int Y);

            [DllImport("user32.dll")]
            static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll")]
            static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

            private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
            private const uint MOUSEEVENTF_LEFTUP = 0x0004;

            internal static bool ReconnectToServer()
            {
                Process[] procs = Process.GetProcessesByName("SCUM");
                if (procs.Length > 0)
                {
                    if (procs[0].MainWindowHandle != IntPtr.Zero)
                    {
                        MoveWindow(procs[0].MainWindowHandle, 0, 0, 800, 600, false);
                        SetForegroundWindow(procs[0].MainWindowHandle);

                        if (GetWindowRect(procs[0].MainWindowHandle, out RECT rect))
                        {
                            int centerX = (rect.Left + rect.Right) / 2;
                            int centerY = (rect.Top + rect.Bottom) / 2;

                            SetCursorPos(centerX, centerY);

                            Thread.Sleep(3000); // Optional delay

                            // Click ok on the center of the screen
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

                            Thread.Sleep(5000); // Optional delay

                            SetCursorPos(centerX - 300, centerY + 80);

                            Thread.Sleep(2000); // Optional delay

                            // Click continue
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                        }

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

            private static void User32_Exited(object? sender, EventArgs e)
            {
                throw new NotImplementedException();
            }

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
