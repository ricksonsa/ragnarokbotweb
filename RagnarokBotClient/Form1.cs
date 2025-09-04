using Shared.Models;
using Shared.Security;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using TheSCUMBot;

namespace RagnarokBotClient
{
    public partial class Form1 : Form
    {
        public static bool Loading = false;
        public static bool IsStopped = true;

        private readonly WebApi _remote;
        private readonly ScumManager _scumManager;

        private Process? _gameProcess;
        private bool _setup = false;

        ConcurrentQueue<BotCommand> _priorityCommandQueue = [];
        ConcurrentQueue<BotCommand> _commandQueue = [];
        private CancellationTokenSource _cancellationTokenSource;
        private string _token;
        private string _exePath;
        private List<ScumServer> _scumServers = [];
        private long _serverId = 0;
        private long _timeToLoadWorld = 30;
        private IniFile _iniFile;
        private string _gameDirPath;

        private string BASE_API_URL = "https://api.thescumbot.com:8082";
        private string BOT_SERVER_ENDPOINT = "api.thescumbot.com";
        private int BOT_SERVER_PORT = 9000;

        private static bool connected = false;
        private BotSocketClient _client;

        private readonly System.Windows.Forms.Timer _pingTimer;

        public Form1()
        {
            InitializeComponent();
            Logger.OnLogging += Logger_OnLogging;
            PasswordBox.UseSystemPasswordChar = true;
            _exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            LoadCredentials();
            _scumManager = new ScumManager();
            LoadIni();
            _remote = new WebApi(new Settings(BASE_API_URL));
            Text += $" - {GetVersion()}";
            Task.Run(CheckVersion);

            _pingTimer = new();
            _pingTimer.Interval = 180000;
            _pingTimer.Tick += PingTimer_Tick;
            _pingTimer.Start();
        }

        private async void PingTimer_Tick(object? sender, EventArgs e)
        {
            if (_client == null || IsStopped) return;

            try
            {
                var result = await _remote.GetAsync<BotState>($"api/bots/{_client.BotId}");

                if (result == null || !result.Connected)
                {
                    Stop();
                    await Task.Delay(1000);
                    Start();
                    return;
                }
            }
            catch { return; }
        }

        private string GetVersion()
        {
            Assembly runningAssembly = Assembly.GetEntryAssembly();
            if (runningAssembly == null)
            {
                runningAssembly = Assembly.GetExecutingAssembly();
            }
            var version = runningAssembly.GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        private async Task CheckVersion()
        {
            try
            {
                var result = await _remote.GetAsync<UpdateResult>("api/application/version");
                if (result.Version != GetVersion())
                {
                    Invoke(new Action(async () =>
                    {
                        var dialogResult = MessageBox.Show("New version available, click ok to update.", "Update", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                        if (dialogResult == DialogResult.OK)
                        {
                            UpdateStatus("Updating version...", force: true);
                            await Task.Delay(1000);
                            StartUpdater(result.Version);
                            Application.Exit();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error)));
            }
        }

        private void StartUpdater(string version)
        {
            string updaterPath = Path.Combine(Application.StartupPath, "Updater.exe");
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = updaterPath,
                    Arguments = version,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadIni()
        {
            _iniFile = new();
            var timeToLoadWorldText = _iniFile.Read("TimeToLoadWorldSeconds");
            if (!string.IsNullOrEmpty(timeToLoadWorldText) && int.TryParse(timeToLoadWorldText, out int timeToLoadWorld))
            {
                _timeToLoadWorld = timeToLoadWorld;
            }
            BOT_SERVER_ENDPOINT = _iniFile.Read("BotServerEndpoint");
            BASE_API_URL = _iniFile.Read("ServerEndpoint");
            var portString = _iniFile.Read("BotServerPort");
            if (int.TryParse(portString, out var port)) BOT_SERVER_PORT = port;
        }

        private async Task SendCheckState(CancellationToken token)
        {
            UpdateStatus("SendCheckState started", false);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_client.BotId))
                    {
                        UpdateStatus("sending check-state", false);
                        var command = new BotCommand();
                        command.Values = [
                        new BotCommandValue
                        {
                            Type = Shared.Enums.ECommandType.SayLocal,
                            Value = $"!check-state-{_client.BotId}"
                        }];
                        _priorityCommandQueue.Enqueue(command);
                    }

                }
                catch (Exception ex)
                {
                    Logger.LogWrite($"SendCheckState Exception: {ex.Message}", write: true);
                }
                await Task.Delay(TimeSpan.FromSeconds(120), token);

            }
        }

        private void Logger_OnLogging(object? sender, string e)
        {
            UpdateStatus(e, false);
        }

        public void LoadCredentials()
        {
            var path = Path.Combine(_exePath, "credentials");
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                EmailBox.Text = lines[0];
                PasswordBox.Text = lines[1];
            }
        }

        public void LoadGameDir()
        {
            var path = Path.Combine(_exePath, "gamedir");
            if (File.Exists(path))
            {
                _gameDirPath = File.ReadAllText(path);
            }
        }

        public async Task Authenticate()
        {
            if (Loading) return;
            UpdateStatus("Connecting to the server.");
            Loading = true;
            AuthResponse? tokenResult = null;
            try
            {
                tokenResult = await _remote.PostAsync<AuthResponse>($"api/authenticate", new
                {
                    Email = EmailBox.Text,
                    Password = PasswordBox.Text,
                    ConfirmPassword = PasswordBox.Text
                });

                if (tokenResult is not null)
                {
                    _token = tokenResult.IdToken;
                    _remote.SetAuthToken(tokenResult.IdToken);
                    _scumServers = tokenResult.ScumServers;
                    foreach (var server in _scumServers)
                    {
                        ServerListBox.Invoke(new Action(() => ServerListBox.Items.Add($"{server.Id} - {server.Name}")));
                    }
                    AuthPanel.Invoke(new Action(() => AuthPanel.Visible = false));
                    AuthPanel.Invoke(new Action(() => AuthPanel.Enabled = false));
                    ServersPanel.Invoke(new Action(() => ServersPanel.Visible = true));
                    ServersPanel.Invoke(new Action(() => ServersPanel.Enabled = true));
                    File.WriteAllText(Path.Combine(_exePath, "credentials"), $"{EmailBox.Text}{Environment.NewLine}{PasswordBox.Text}");
                }
                Loading = false;
            }
            catch (Exception ex)
            {
                Loading = false;
                AuthFeedback.Invoke(new Action(() => AuthFeedback.Visible = true));
                tokenResult = null;
                Logger.LogWrite($"Failed to connect: {ex.Message}", write: true);
            }
            finally
            {
                if (InvokeRequired)
                    Invoke(() => LoginButton.Enabled = true);
                else
                    LoginButton.Enabled = true;
            }

            return;
        }

        public async Task Login()
        {

            if (_token is null) return;
            AuthResponse? tokenResult = null;
            try
            {
                tokenResult = await _remote.GetAsync<AuthResponse>($"api/login?serverId={_serverId}");
            }
            catch (Exception ex)
            {
                Loading = false;
                AuthFeedback.Invoke(new Action(() => AuthFeedback.Visible = true));
                tokenResult = null;
                Logger.LogWrite($"Failed to connect: {ex.Message}", write: true);
            }

            if (tokenResult is not null)
            {
                UpdateStatus("Connected.");
                UpdateStatus("Waiting.");
                _token = tokenResult.AccessToken;
                _remote.SetAuthToken(tokenResult.AccessToken);
                ServersPanel.Invoke(new Action(() => ServersPanel.Visible = false));
                ServersPanel.Invoke(new Action(() => ServersPanel.Enabled = false));
                _ = ProcessCommand();
            }
            Loading = false;

            return;
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            LoginButton.Enabled = false;
            Task.Run(Authenticate);
        }

        private void UpdateStatusSafe(string message)
        {
            if (InvokeRequired)
                Invoke(() => UpdateStatus(message));
            else
                UpdateStatus(message);
        }

        public void Start()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource!.Token;
            _scumManager.Token = token;

            try
            {
                StartButton.Invoke(() => StartButton.Text = "Stop");

                Task.Run(async () =>
                {
                    try
                    {
                        if (await HealthCheck(token))
                        {
                            await GameCheck(token);
                            await _scumManager.ReconnectToServer();
                            await Task.Delay(TimeSpan.FromSeconds(Math.Max(_timeToLoadWorld, 1)), token);

                            await _client.ConnectAsync(token);
                            IsStopped = false;
                            _ = SendCheckState(token);
                            UpdateStatusSafe("Started...");
                        }
                        else
                        {
                            Stop();
                            UpdateStatusSafe("Server did not respond.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Stop();
                        Logger.LogWrite($"Error: {ex.Message}", write: true);
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            connected = false;
            if (StatusValue.InvokeRequired)
            {
                StartButton.Invoke(new Action(() => StartButton.Text = "Start"));
            }
            else
            {
                StartButton.Text = "Start";
            }
            IsStopped = true;
            UpdateStatus("Stopped.");
        }

        private async Task<bool> HealthCheck(CancellationToken token)
        {
            UpdateStatus("Checking server status.");
            token.ThrowIfCancellationRequested();

            try
            {
                var result = await _remote.GetAsync("healthz");
                return result == "Healthy";
            }
            catch
            {
                Logger.LogWrite($"Server is not available", write: true);
                return false;
            }
        }

        private async Task GameCheck(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var processes = Process.GetProcessesByName("SCUM");
            UpdateStatus("Waiting for SCUM process.");
            //if (!processes.Any() && !string.IsNullOrEmpty(_gameDirPath))
            //{
            //    Process.Start(_gameDirPath);
            //    await Task.Delay(5000, token);
            //}
            while (!processes.Any())
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(500, token);
                continue;
            }
            _gameProcess = processes.First();
            //try
            //{
            //    _gameProcess.EnableRaisingEvents = true;

            //}
            //catch (Exception) { }
            //_gameDirPath = Path.Combine(_gameProcess.StartInfo.WorkingDirectory, _gameProcess.StartInfo.FileName);
            //try
            //{
            //    File.WriteAllText(Path.Combine(_exePath, "gamedir"), _gameDirPath);
            //}
            //catch (Exception) { }

            _gameProcess.Exited += ScumProcess_Exited;
            UpdateStatus($"SCUM process found with PID: {_gameProcess.Id}.", false);
            UpdateStatus("Attempting to connect to game server.");

            return;
        }

        private async Task ProcessCommand()
        {
            while (true)
            {
                try
                {
                    if (_priorityCommandQueue.TryDequeue(out BotCommand? priorityCommand) && priorityCommand is not null)
                    {
                        var tasks = new CommandHandler(_scumManager, _remote).Handle(priorityCommand);
                        if (tasks != null && tasks.Count > 0)
                        {
                            foreach (var task in tasks)
                            {
                                UpdateStatus("Processing command.");
                                await task();
                            }
                        }
                    }

                    if (_commandQueue.TryDequeue(out BotCommand? command) && command is not null)
                    {
                        if (command.Values.Any(cmd => cmd.Type == Shared.Enums.ECommandType.Reconnect))
                        {
                            UpdateStatus("Bot flagged for reconnect.");
                            await _scumManager.ReconnectToServer();
                            await Task.Delay(TimeSpan.FromSeconds(Math.Max(_timeToLoadWorld, 1)));
                            _priorityCommandQueue.Enqueue(new BotCommand
                            {
                                Values = [
                                new BotCommandValue
                                {
                                    Type = Shared.Enums.ECommandType.SayLocal,
                                    Value = $"!check-state-{_client.BotId}"
                                }]
                            });
                        }
                        else
                        {
                            var tasks = new CommandHandler(_scumManager, _remote).Handle(command);
                            if (tasks != null && tasks.Count > 0)
                            {
                                foreach (var task in tasks)
                                {
                                    UpdateStatus("Processing command.");
                                    await task();
                                }
                            }
                        }

                    }
                }
                catch (Exception) { }
                await Task.Delay(1000);
            }
        }

        private void ScumProcess_Exited(object? sender, EventArgs e)
        {
            UpdateStatus("SCUM process closed.");
            _gameProcess!.Exited -= ScumProcess_Exited;
            _gameProcess = null;
            Stop();
        }

        private void UpdateStatus(string status, bool changeLabel = true, bool force = false)
        {
            var action = new Action(() =>
            {
                var text = $"{new DateTimeOffset(DateTime.Now)} {status}";
                LogBox.AppendText(text + Environment.NewLine);
                LogBox.SelectionStart = LogBox.TextLength;
                LogBox.ScrollToCaret();
            });
            var scrollAction = new Action(LogBox.ScrollToCaret);
            var clearTextAction = new Action(() =>
            {
                if (LogBox.Text.Length >= 15000) LogBox.Text = string.Empty;
            });
            if (StatusValue.InvokeRequired)
            {
                if (changeLabel) StatusValue.Invoke(new Action(() => StatusValue.Text = status));
                if (debugCheckBox.Checked && !force)
                {
                    StatusValue.Invoke(action);
                    StatusValue.Invoke(clearTextAction);
                    StatusValue.Invoke(scrollAction);
                }
            }
            else
            {
                if (changeLabel) StatusValue.Text = status;
                if (debugCheckBox.Checked || force)
                {
                    action?.Invoke();
                    clearTextAction?.Invoke();
                    scrollAction?.Invoke();
                }
            }
        }

        private void StatusValue_Click(object sender, EventArgs e)
        {

        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (StartButton.Text == "Start")
            {
                Start();

            }
            else
            {
                Stop();
            }
        }

        private void TestBtn_Click(object sender, EventArgs e)
        {
            UpdateStatus("Added test command", false);
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void EmailBox_TextChanged(object sender, EventArgs e) { }

        private void label1_Click(object sender, EventArgs e) { }

        private async void ServerListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ServerListBox.SelectedIndex == -1) return;
            var test = ((dynamic)sender).SelectedItem as string;
            _serverId = long.Parse(test!.Split(" - ")[0]);

            ServersPanel.Visible = false;
            ServersPanel.Enabled = false;

            InitializeSocketClient();
            await Login();
        }

        private void InitializeSocketClient()
        {
            _client = new(BOT_SERVER_ENDPOINT, BOT_SERVER_PORT, _serverId)
            {
                Remote = _remote
            };
            _client.OnMessageReceived += Client_OnMessageReceived;
        }

        private void Client_OnMessageReceived(object? sender, BotCommand e)
        {
            _commandQueue.Enqueue(e);
        }

        private void thescumbotSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Mark the link as visited
            thescumbotSite.LinkVisited = true;

            // Open the URL in the default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = thescumbotSite.Text,
                UseShellExecute = true // This ensures the default browser is used
            });
        }
    }
}
