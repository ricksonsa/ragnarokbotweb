using FluentFTP;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Timer = System.Timers.Timer;

namespace RagnarokBotWeb.HostedServices
{
    public abstract class BaseHostedService
    {
        private readonly FtpClient _ftpClient;
        private readonly string _baseFileName;
        private readonly Dictionary<string, Line> _processedLines = [];
        private readonly IServiceProvider _services;
        public static Timer Timer;

        public BaseHostedService(IServiceProvider serviceProvider, FtpClient ftpClient, string baseFileName, int seconds = 10)
        {
            _ftpClient = ftpClient;
            _baseFileName = baseFileName;

            Timer = new Timer(TimeSpan.FromSeconds(seconds));
            Timer.Elapsed += async (sender, e) => await Process();
            Timer.AutoReset = true;
            Timer.Enabled = true;
            _services = serviceProvider;

            using (var scope = serviceProvider.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var readings = uow.Readings.Where(reader => reader.CreateDate.Date == DateTime.Now.Date).ToList();
                if (readings.Any())
                {
                    _processedLines = readings
                      .Select(reader => new Line(reader.Value, reader.FileName, reader.Hash))
                      .DistinctBy(x => x.Hash)
                      .ToDictionary(value => value.Hash);
                }
            }
        }

        private async Task SaveLine(Line line)
        {
            using (var scope = _services.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await uow.Readings.AddAsync(new Reader(line.File, line.Value, line.Hash));
                await uow.SaveAsync();
            }
        }

        public abstract Task Process();

        public IEnumerable<string> GetLogFiles()
        {
            var timeStampYesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            var timeStampToday = DateTime.Now.ToString("yyyyMMdd");
            var timeStampTomorrow = DateTime.Now.AddDays(1).ToString("yyyyMMdd");
            var files = _ftpClient.GetNameListing("/189.1.169.132_7000/");
            return files.ToList()
                .Where(fileName =>
                fileName.StartsWith(_baseFileName + timeStampYesterday) || fileName.StartsWith(_baseFileName + timeStampToday) || fileName.StartsWith(_baseFileName + timeStampTomorrow))
                .OrderBy(x => DateTime.ParseExact(x.Split("_")[1].Replace(".log", string.Empty), "yyyyMMddHHmmss", null));
        }

        public IList<Line> GetUnreadFileLines(string fileName)
        {
            using (var stream = _ftpClient.OpenRead("/189.1.169.132_7000/" + fileName, FtpDataType.ASCII))
            using (var reader = new StreamReader(stream))
            {
                string line;
                IList<Line> lines = [];

                while ((line = reader.ReadLine()) != null)
                {
                    var fixedLine = line.Replace("\0", "");
                    var lineObject = new Line(fixedLine, fileName);
                    if (!_processedLines.ContainsKey(lineObject.Hash))
                    {
                        lines.Add(lineObject);
                        _processedLines.Add(lineObject.Hash, lineObject);
                        SaveLine(lineObject);
                    }
                }

                return lines;
            }
        }
    }
}
