using Newtonsoft.Json;
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

        private readonly WebApi _remote;
        private readonly ScumManager _scumManager;

        private Process _gameProcess;
        private bool _setup = false;
        private string _identifier;

        ConcurrentQueue<BotCommand> _commandQueue = [];
        private CancellationTokenSource _cancellationTokenSource;
        private string _token;
        private string _exePath;
        private List<ScumServer> _scumServers = [];
        private long _serverId = 0;
        private long _timeToLoadWorld = 30;
        private IniFile _iniFile;

        private const string BASE_URL = "https://api.thescumbot.com:8082";

        public Guid Guid { get; set; }

        public Form1()
        {
            InitializeComponent();
            Logger.OnLogging += Logger_OnLogging;
            _identifier = Environment.UserName;
            PasswordBox.UseSystemPasswordChar = true;
            _exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            LoadCredentials();
            _scumManager = new ScumManager();
            Guid = Guid.NewGuid();
            _remote = new WebApi(new Settings(BASE_URL));
            LoadIni();
            Text += $" - {GetVersion()}";
            Task.Run(CheckVersion);
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
                    var dialogResult = MessageBox.Show("New version available, click ok to update.", "Update", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                    if (dialogResult == DialogResult.OK)
                    {
                        await DownloadVersion(result.Version);
                        UpdateStatus("Updating version...", force: true);
                        await Task.Delay(1000);
                        StartUpdater();
                        Application.Exit();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            }

        }

        private async Task DownloadVersion(string version)
        {
            string zipPath = Path.Combine(Path.GetTempPath(), "thescumbot.zip");
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync($"{BASE_URL}/images/thescumbot-{version}.zip"))
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            using (FileStream fileStream = new FileStream(zipPath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }
        }

        private void StartUpdater()
        {
            string updaterPath = Path.Combine(Application.StartupPath, "Updater.exe");
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = updaterPath,
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
        }

        private async Task CheckStatusLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var bot = await _remote.GetAsync<BotUser>($"api/bots/guid/{Guid}");
                    if (bot != null)
                    {
                        var diff = (DateTime.UtcNow - bot.LastPinged!.Value).TotalMinutes;
                        if (diff >= 5)
                        {
                            _ = _scumManager.ReconnectToServer();
                            await _remote.PostAsync($"api/bots/register?guid={Guid}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("CheckStatusLoop Exception:", false);
                    UpdateStatus(ex.Message, false);
                }

                await Task.Delay(TimeSpan.FromSeconds(60), token);
            }
        }

        private async Task CanStartReceivingCommandsCheck(CancellationToken token)
        {
            UpdateStatus("Waiting for server confirmation.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var bot = await _remote.GetAsync<BotUser>($"api/bots/guid/{Guid}");
                    if (bot != null && bot.LastPinged.HasValue) return;
                }
                catch (Exception ex)
                {
                    UpdateStatus("CanStartReceivingCommandsCheck Exception:", false);
                    UpdateStatus(ex.Message, false);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }

        private async Task SendCheckState(CancellationToken token)
        {
            UpdateStatus("SendCheckState started", false);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    UpdateStatus("sending check-state", false);
                    var command = new BotCommand();
                    command.Values = [
                    new BotCommandValue
                    {
                        Type = Shared.Enums.ECommandType.SayLocal,
                        Value = $"!check-state-{Guid}"
                    }];
                    _commandQueue.Enqueue(command);
                    await Task.Delay(TimeSpan.FromSeconds(120), token);
                }
                catch (Exception ex)
                {
                    UpdateStatus("SendCheckState Exception:", false);
                    UpdateStatus(ex.Message, false);
                }

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
                Debug.WriteLine(ex.Message);
                UpdateStatus("Failed to connect.", force: true);
                UpdateStatus(ex.Message, force: true);
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
                Debug.WriteLine(ex.Message);
                UpdateStatus("Failed to connect.", force: true);
                UpdateStatus(ex.Message, force: true);
            }

            if (tokenResult is not null)
            {
                UpdateStatus("Connected.");
                UpdateStatus("Waiting.");
                _token = tokenResult.AccessToken;
                _remote.SetAuthToken(tokenResult.AccessToken);
                ServersPanel.Invoke(new Action(() => ServersPanel.Visible = false));
                ServersPanel.Invoke(new Action(() => ServersPanel.Enabled = false));
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

        public Task RegisterBot()
        {
            return _remote.PostAsync($"api/bots/register?guid={Guid}");
        }

        public void Start()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                return; // already running

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;
                _scumManager.Token = token;

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
                            await RegisterBot();

                            _ = Task.Run(() => CheckStatusLoop(token));
                            _ = Task.Run(() => ProcessCommand(token));
                            _ = Task.Run(() => SendCheckState(token));

                            await CanStartReceivingCommandsCheck(token);
                            _ = ReceiveCommand(token);

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
                        UpdateStatusSafe($"Unexpected error: {ex.Message}");
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
            _ = _remote.DeleteAsync($"api/bots/unregister?guid={Guid}");
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            if (StatusValue.InvokeRequired)
            {

                StartButton.Invoke(new Action(() => StartButton.Text = "Start"));
            }
            else
            {
                StartButton.Text = "Start";
            }
            UpdateStatus("Stopped.");
            UpdateStatus("Waiting.");
        }

        private Task<bool> HealthCheck(CancellationToken token)
        {
            UpdateStatus("Checking server status.");
            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

            return _remote.GetAsync("healthz").ContinueWith((val) =>
            {
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

                try
                {
                    if (val.Result == "Healthy")
                    {
                        return true;
                    }
                    else
                    {
                        UpdateStatus("Could not connect to server.");
                        return false;
                    }
                }
                catch (Exception)
                {
                    UpdateStatus("Could not connect to server.");
                    return false;
                }
            });
        }

        private async Task GameCheck(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var processes = Process.GetProcessesByName("SCUM");
            UpdateStatus("Waiting for SCUM process.");
            while (!processes.Any())
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(500, token);
                continue;
            }
            _gameProcess = processes.First();
            _gameProcess.Exited += ScumProcess_Exited;
            UpdateStatus($"SCUM process found with PID: {_gameProcess.Id}.", false);
            UpdateStatus("Attempting to connect to game server.");

            return;
        }

        private async Task ReceiveCommand(CancellationToken token)
        {
            UpdateStatus("Server connection confirmed.");
            UpdateStatus("Receiving commands.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var command = await _remote.GetAsync($"api/bots/commands?guid={Guid}");

                    if (!string.IsNullOrEmpty(command))
                    {
                        _commandQueue.Enqueue(JsonConvert.DeserializeObject<BotCommand>(command)!);
                    }
                    await Task.Delay(3000, token);
                }
                catch (Exception) { }

            }
        }

        private async Task ProcessCommand(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_commandQueue.TryDequeue(out BotCommand command))
                    {
                        var tasks = new CommandHandler(_scumManager, _remote).Handle(command);
                        if (tasks == null || tasks.Count == 0) continue;

                        foreach (var task in tasks)
                        {
                            await task();
                        }

                        await Task.Delay(2000, token);
                    }

                }
                catch (Exception) { }

            }
        }

        private void ScumProcess_Exited(object? sender, EventArgs e)
        {
            UpdateStatus("SCUM process closed.");
            _gameProcess.Exited -= ScumProcess_Exited;
            Stop();
        }

        private void UpdateStatus(string status, bool changeLabel = true, bool force = false)
        {
            var action = new Action(() => LogBox.Text += $"\n {new DateTimeOffset(DateTime.Now)} {status}");
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
                }
            }
            else
            {
                if (changeLabel) StatusValue.Text = status;
                if (debugCheckBox.Checked && !force)
                {
                    action?.Invoke();
                    clearTextAction?.Invoke();
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

        private void ServerListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ServerListBox.SelectedIndex == -1) return;
            var test = ((dynamic)sender).SelectedItem as string;
            _serverId = long.Parse(test!.Split(" - ")[0]);
            ServersPanel.Invoke(new Action(() => ServersPanel.Visible = false));
            ServersPanel.Invoke(new Action(() => ServersPanel.Enabled = false));


            Login();
        }

        private void thescumbotSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Mark the link as visited
            thescumbotSite.LinkVisited = true;

            // Open the URL in the default browser
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = thescumbotSite.Text,
                UseShellExecute = true // This ensures the default browser is used
            });
        }
    }
}
