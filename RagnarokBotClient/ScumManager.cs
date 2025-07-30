using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RagnarokBotClient
{
    public class ScumManager
    {
        private readonly IniFile _iniFile = new();
        private string chatKey = "t";
        private int delayBetweenInteractions = 0;

        public CancellationToken Token { get; set; }

        public ScumManager()
        {
            SetupFromIni();
        }

        private void SetupFromIni()
        {
            try
            {
                chatKey = _iniFile.Read("OpenChatKey");
                if (int.TryParse(_iniFile.Read("MsDelayBetweenInteractions"), out int delay))
                    delayBetweenInteractions = delay;

                if (string.IsNullOrEmpty(chatKey)) chatKey = "t";
            }
            catch (Exception)
            {
            }
        }

        private Task Sleep(int ms = 600)
        {
            return Task.Delay(ms + delayBetweenInteractions);
        }

        private Task RunCommand(string command, int tab = 0)
        {
            if (!User32.BringWindowToForeground()) { return Task.CompletedTask; }

            Sleep().Wait();

            SendKeys.SendWait(chatKey);
            Token.ThrowIfCancellationRequested();

            Sleep().Wait();

            switch (tab)
            {
                case 0:
                default:
                    break;
                case 1:
                    SendKeys.SendWait("{tab}");
                    Sleep().Wait();
                    break;
            }

            var setClipboardThread = new Thread(() => Clipboard.SetText(command));
            setClipboardThread.SetApartmentState(ApartmentState.STA);
            setClipboardThread.Start();
            setClipboardThread.Join(); // safer than WaitOne here

            Sleep().Wait();
            SendKeys.SendWait("^{v}");
            Sleep(250).Wait();

            Token.ThrowIfCancellationRequested();

            SendKeys.SendWait("{enter}");
            Sleep().Wait();

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
            Sleep().Wait();

            SendKeys.SendWait("{escape}");

            Sleep().Wait();
            return Task.CompletedTask;
        }

        public static Task Start()
        {
            if (!User32.ReconnectToServer()) { return Task.CompletedTask; }

            return Task.CompletedTask;
        }

        public Task Teleport(string steamID, string x, string y, string z)
        {
            Logger.LogWrite($"Teleporting {steamID} to {x}, {y}, {z}");
            return RunCommand($"#teleport {x} {y} {z} {steamID}");
        }

        public Task Teleport(string steamID, string coordinates)
        {
            Logger.LogWrite($"Teleporting {steamID} to {coordinates}");
            return RunCommand($"#teleport {coordinates} {steamID}");
        }

        public Task TeleportTo(string adminSteamID, string playerSteamID)
        {
            Logger.LogWrite($"Teleporting {adminSteamID} to {playerSteamID}");
            return RunCommand($"#teleportto {playerSteamID} {adminSteamID}");
        }

        public Task TeleportBotTo(string playerSteamID)
        {
            Logger.LogWrite($"Teleporting bot to {playerSteamID}");
            return RunCommand($"#teleportto {playerSteamID}");
        }

        public Task SpawnItem(string item, int amount)
        {
            Logger.LogWrite($"Spawning item {amount} x {item}");
            return RunCommand($"#spawnitem {item} {amount}");
        }

        public Task SpawnItem(string item, int amount, string location)
        {
            Logger.LogWrite($"Spawning item {amount} x {item} at location {location}");
            return RunCommand($"#spawnitem {item} {amount} location {location}");
        }

        public Task SpawnItem(string item, int amount, int ammoCount, string location)
        {
            Logger.LogWrite($"Spawning item {amount} x {item} with AmmoCount {ammoCount} at location {location}");
            return RunCommand($"#spawnitem {item} {amount} AmmoCount {ammoCount} location {location}");
        }

        public Task KickPlayer(string steamId)
        {
            Logger.LogWrite($"Kicking player {steamId} from the server");
            return RunCommand($"#kick {steamId}");
        }

        public Task BanPlayer(string steamId)
        {
            Logger.LogWrite($"Banning player {steamId} from the server");
            return RunCommand($"#ban {steamId}");
        }

        public Task Say(string text)
        {
            if (text.StartsWith("#")) text.TrimStart('#');
            Logger.LogWrite($"Bot says: {text}");
            return RunCommand($"{text}", tab: 1);
        }

        public Task SayLocal(string text)
        {
            Logger.LogWrite($"Bot says locally: {text}");
            return RunCommand($"{text}");
        }

        public Task Command(string text)
        {
            if (!text.StartsWith("#"))
            {
                Logger.LogWrite($"Invalid command {text}");
                return Task.CompletedTask;
            }
            Logger.LogWrite($"Running command {text}");
            return RunCommand($"{text}");
        }

        public Task Announce(string content)
        {
            Logger.LogWrite($"Announcing {content}");
            return RunCommand($"#announce {content}");
        }

        public Task DumpAllSquadsInfoList()
        {
            Logger.LogWrite($"DumpAllSquadsInfoList");
            return RunCommand("#DumpAllSquadsInfoList");
        }

        public Task DumpListPlayers()
        {
            Logger.LogWrite($"DumpListPlayers");
            return RunCommand("#ListPlayers true");
        }

        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            struct INPUT
            {
                public uint type;
                public InputUnion u;
            }

            [StructLayout(LayoutKind.Explicit)]
            struct InputUnion
            {
                [FieldOffset(0)] public KEYBDINPUT ki;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct KEYBDINPUT
            {
                public ushort wVk;
                public ushort wScan;
                public uint dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            const uint INPUT_KEYBOARD = 1;
            const uint KEYEVENTF_KEYUP = 0x0002;

            const ushort VK_CONTROL = 0x11;
            const ushort VK_V = 0x56;

            [DllImport("user32.dll", SetLastError = true)]
            static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

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

            internal static void SendCtrlV()
            {
                INPUT[] inputs = new INPUT[4];

                // Ctrl down
                inputs[0] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = VK_CONTROL,
                            dwFlags = 0,
                        }
                    }
                };

                // V down
                inputs[1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = VK_V,
                            dwFlags = 0,
                        }
                    }
                };

                // V up
                inputs[2] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = VK_V,
                            dwFlags = KEYEVENTF_KEYUP,
                        }
                    }
                };

                // Ctrl up
                inputs[3] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = VK_CONTROL,
                            dwFlags = KEYEVENTF_KEYUP,
                        }
                    }
                };

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            }


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
