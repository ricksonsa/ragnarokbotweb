using Newtonsoft.Json;
using Shared.Models;
using Shared.Security;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace RagnarokBotClient
{
    public partial class Form1 : Form
    {
        public static bool Loading = false;

        private readonly WebApi _remote;
        private Process _gameProcess;
        private bool _setup = false;
        private string _identifier;

        ConcurrentQueue<BotCommand> _commandQueue = [];
        private CancellationTokenSource _cancellationTokenSource;
        private string _token;
        private string _exePath;
        private List<ScumServer> _scumServers = [];
        private long _serverId = 0;

        public Form1()
        {
            InitializeComponent();
            _remote = new WebApi(new Settings("http://localhost:5000"));
            Logger.OnLogging += Logger_OnLogging;
            _identifier = Environment.UserName;
            PasswordBox.UseSystemPasswordChar = true;
            _exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            ServersPanel.Size = new Size(776, 429);
            LoadCredentials();
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
            UpdateStatus("Connecting to the server...");
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
            }
            catch (Exception ex)
            {
                Loading = false;
                AuthFeedback.Invoke(new Action(() => AuthFeedback.Visible = true));
                tokenResult = null;
                Debug.WriteLine(ex.Message);
                UpdateStatus("Failed to connect.");
            }

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
                UpdateStatus("Failed to connect.");
            }

            if (tokenResult is not null)
            {
                UpdateStatus("Connected.");
                UpdateStatus("Waiting...");
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
            Task.Run(async () =>
            {
                await Authenticate();
            });
        }

        public void Start()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;
                StartButton.Text = "Stop";
                HealthCheck(token)
                    .ContinueWith((_) => GameCheck(token))
                    .ContinueWith((_) => Task.WaitAll(ReceiveCommand(token), ProcessCommand(token)));


            }
            catch (OperationCanceledException)
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        public void Stop()
        {
            _remote.DeleteAsync($"api/bots/unregister");
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            if (StatusValue.InvokeRequired)
            {

                StartButton.Invoke(new Action(() => StartButton.Text = "Start"));
            }
            else
            {
                StartButton.Text = "Start";
            }
            UpdateStatus("Stopped.");
            UpdateStatus("Waiting...");
        }

        private Task<bool> HealthCheck(CancellationToken token)
        {
            UpdateStatus("Checking server status...");
            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

            return _remote.GetAsync("healthz").ContinueWith((val) =>
            {
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

                try
                {
                    if (val.Result == "Healthy")
                    {
                        _remote.PostAsync($"api/bots/register", null).Wait();
                        return true;
                    }
                    else
                    {
                        UpdateStatus("Could not connect to server.");
                        Stop();
                        return false;
                    }
                }
                catch (Exception)
                {
                    UpdateStatus("Could not connect to server.");
                    Stop();
                    return false;
                }
            });
        }

        private async Task GameCheck(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var processes = Process.GetProcessesByName("SCUM");
            UpdateStatus("Waiting for SCUM process...");
            while (!processes.Any())
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(500, token);
                continue;
            }
            _gameProcess = processes.First();
            _gameProcess.Exited += ScumProcess_Exited;
            UpdateStatus($"SCUM process found with PID: {_gameProcess.Id}.", false);
            if (!_setup)
            {
                // await ScumManager.SetupBot();
                _setup = true;
            }
            return;
        }

        private async Task ReceiveCommand(CancellationToken token)
        {
            await _remote.PostAsync($"api/bots/register", null);
            UpdateStatus("Ready...");
            UpdateStatus("Listening for commands...");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var command = await _remote.GetAsync($"api/bots/commands");
                    if (!string.IsNullOrEmpty(command))
                    {
                        _commandQueue.Enqueue(JsonConvert.DeserializeObject<BotCommand>(command)!);
#if DEBUG
                        Debug.WriteLine("Command Received");
#endif
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
#if DEBUG
                Debug.WriteLine("Command Queue state is:");
                Debug.WriteLine(JsonConvert.SerializeObject(_commandQueue.ToList()));
#endif
                try
                {
                    if (_commandQueue.TryDequeue(out BotCommand command))
                    {
                        var tasks = new CommandHandler(_cancellationTokenSource.Token, _identifier, _remote).Handle(command);
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

        private void UpdateStatus(string status, bool changeLabel = true)
        {
            if (StatusValue.InvokeRequired)
            {
                if (changeLabel) StatusValue.Invoke(new Action(() => StatusValue.Text = status));
                StatusValue.Invoke(new Action(() => LogBox.Text += "\n" + status));
            }
            else
            {
                if (changeLabel) StatusValue.Text = status;
                LogBox.Text += "\n" + status;
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
            var test = ((dynamic)sender).SelectedItem as string;
            _serverId = long.Parse(test!.Split(" - ")[0]);
            ServersPanel.Invoke(new Action(() => ServersPanel.Visible = false));
            ServersPanel.Invoke(new Action(() => ServersPanel.Enabled = false));
            Login();
        }
    }
}
