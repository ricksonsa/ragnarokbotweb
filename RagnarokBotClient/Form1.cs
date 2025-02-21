using Newtonsoft.Json;
using Shared.Enums;
using System.Diagnostics;
using System.Reflection;

namespace RagnarokBotClient
{
    public partial class Form1 : Form
    {
        private readonly WebApi _remote;
        private Process _gameProcess;
        private bool _setup = false;
        private string _identifier;
        private string _exePath;

        Queue<Command> _commandQueue = [];
        private CancellationTokenSource _cancellationTokenSource;

        public Form1()
        {
            InitializeComponent();
            _remote = new WebApi(new Settings("https://localhost:7257"));
            Logger.OnLogging += Logger_OnLogging;
            UpdateStatus("Waiting...");
            _identifier = Environment.UserName;
            _exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        }

        private void Logger_OnLogging(object? sender, string e)
        {
            UpdateStatus(e, false);
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
                    .ContinueWith((_) => { ProcessComand(token); ReceiveCommand(token); });
            }
            catch (OperationCanceledException)
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        public void Stop()
        {
            _remote.DeleteAsync($"api/bots/unregister?identifier={_identifier}");
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

        private Task HealthCheck(CancellationToken token)
        {
            UpdateStatus("Connecting to server...");
            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
            return _remote.GetAsync("healthz").ContinueWith((val) =>
            {
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

                try
                {
                    if (val.Result == "Healthy")
                    {
                        _remote.PostAsync($"api/bots/register?identifier={_identifier}", null).Wait();
                        UpdateStatus("Connected.");
                    }
                    else
                    {
                        UpdateStatus("Could not connect to server.");
                        Stop();
                    }
                }
                catch (Exception)
                {
                    UpdateStatus("Could not connect to server.");
                    Stop();
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
            token.ThrowIfCancellationRequested();
            UpdateStatus("Ready...");
            UpdateStatus("Listening for commands...");
            while (!token.IsCancellationRequested)
            {
                var command = await _remote.GetAsync($"api/bots/commands?identifier={_identifier}");
                if (command is not null)
                {
                    _commandQueue.Enqueue(JsonConvert.DeserializeObject<Command>(command)!);
                }
                await Task.Delay(1000, token);
            }
        }

        private async Task ProcessComand(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_commandQueue.TryDequeue(out Command command))
                {
                    if (command is null) continue;
                    var task = new CommandHandler(_cancellationTokenSource.Token, _identifier, _remote).Handle(command);
                    if (task is null) continue;

                    await task!;
                    while (!task.IsCompleted) { continue; }
                }

                await Task.Delay(2000, token);
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
            var command = new Command
            {
                Value = "Weapon_MK18",
                Type = ECommandType.Delivery,
                Amount = 1,
                Target = "CastigoDeMae"
            };
            _commandQueue.Enqueue(command);
        }
    }
}
