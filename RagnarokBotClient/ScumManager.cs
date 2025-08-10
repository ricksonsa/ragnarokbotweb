using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RagnarokBotClient
{
    public class ScumManager
    {
        private readonly IniFile _iniFile = new();
        private string chatKey = "t";
        private int delayBetweenInteractions = 0;
        private bool firstRun = true;

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

            switch (tab)
            {
                case 0:
                default:
                    Sleep(1000).Wait();
                    break;
                case 1:
                    Sleep(1000).Wait();
                    SendKeys.SendWait("{tab}");
                    Sleep(1000).Wait();
                    SendKeys.SendWait("{tab}");
                    Sleep(1000).Wait();
                    break;
            }

            SendKeys.SendWait("{escape}");

            Sleep().Wait();
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

        public Task TeleportBotToCoordinates(string coordinates)
        {
            Logger.LogWrite($"Teleporting bot to {coordinates}");
            return RunCommand($"#teleport {coordinates}");
        }

        public Task TeleportBotToPlayer(string steamId)
        {
            Logger.LogWrite($"Teleporting bot to {steamId}");
            return RunCommand($"#teleportto {steamId}");
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

        public Task DumpAllFlagsInfoList(int page)
        {
            Logger.LogWrite($"DumpAllFlagsInfoList");
            return RunCommand($"#ListFlags {page} true");
        }

        public Task DumpListPlayers()
        {
            //Logger.LogWrite($"DumpListPlayers");
            return RunCommand("#ListPlayers true");
        }

        public Task ReconnectToServer()
        {
            Logger.LogWrite($"Connecting to server");
            var task = User32.ReconnectToServer(firstRun);
            firstRun = false;
            return task;
        }

        public Task ChangeCurrency(string type, string target, string value)
        {
            Logger.LogWrite($"ChangeCurrencyBalance {type} {value} {target}");
            return RunCommand($"#ChangeCurrencyBalance {type} {value} {target}");
        }

        public Task ChangeFame(string target, string value)
        {
            Logger.LogWrite($"ChangeFamePoints {value} {target}");
            return RunCommand($"#ChangeFamePoints {value} {target}");
        }

        private class User32
        {

            [StructLayout(LayoutKind.Sequential)]
            struct KEYBDINPUT
            {
                public ushort wVk;
                public ushort wScan;
                public uint dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [StructLayout(LayoutKind.Explicit)]
            struct InputUnion
            {
                [FieldOffset(0)] public MOUSEINPUT mi;
                [FieldOffset(0)] public KEYBDINPUT ki;
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

            [StructLayout(LayoutKind.Sequential)]
            struct INPUT
            {
                public uint type;
                public InputUnion u;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct MOUSEINPUT
            {
                public int dx;
                public int dy;
                public uint mouseData;
                public uint dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }


            const uint INPUT_MOUSE = 0;
            const uint MOUSEEVENTF_MOVE = 0x0001;
            const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
            const uint MOUSEEVENTF_LEFTUP = 0x0004;

            private static void SendCtrlD()
            {
                var inputs = new INPUT[4];

                // Ctrl down
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].u.ki = new KEYBDINPUT
                {
                    wVk = 0x11, // VK_CONTROL
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                // D down
                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].u.ki = new KEYBDINPUT
                {
                    wVk = 0x44, // 'D'
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                // D up
                inputs[2].type = INPUT_KEYBOARD;
                inputs[2].u.ki = new KEYBDINPUT
                {
                    wVk = 0x44,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                // Ctrl up
                inputs[3].type = INPUT_KEYBOARD;
                inputs[3].u.ki = new KEYBDINPUT
                {
                    wVk = 0x11,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            }

            private static void SendMouseClick()
            {
                var inputs = new INPUT[2];

                // Left button down
                inputs[0].type = INPUT_MOUSE;
                inputs[0].u.mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTDOWN,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                // Left button up
                inputs[1].type = INPUT_MOUSE;
                inputs[1].u.mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            }


            public static async Task ReconnectToServer(bool firstRun)
            {
                Process[] procs = Process.GetProcessesByName("SCUM");
                if (procs.Length > 0)
                {
                    if (procs[0].MainWindowHandle != IntPtr.Zero)
                    {
                        MoveWindow(procs[0].MainWindowHandle, 0, 0, 800, 600, false);
                        SetForegroundWindow(procs[0].MainWindowHandle);
                        await Task.Delay(2000);

                        if (GetWindowRect(procs[0].MainWindowHandle, out RECT rect))
                        {
                            int centerX = (rect.Left + rect.Right) / 2;
                            int centerY = ((rect.Top + rect.Bottom) / 2);

                            SetCursorPos(centerX, centerY + 25);
                            await Task.Delay(300);

                            if (firstRun) SendCtrlD();

                            SendMouseClick(); // Click "OK"
                            await Task.Delay(3000);

                            SetCursorPos(centerX, centerY);
                            await Task.Delay(300);

                            SetCursorPos(centerX - 300, centerY + 60);
                            await Task.Delay(300);

                            SendMouseClick(); // Click "Continue"
                        }
                    }
                }
                else
                {
                    Logger.LogWrite($"Couldn't find SCUM Window.");
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
