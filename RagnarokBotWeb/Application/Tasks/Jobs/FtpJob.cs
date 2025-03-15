using FluentFTP;
using Quartz;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public abstract class FtpJob
    {
        private readonly IFtpService _ftpService;
        private readonly string _baseFileName;
        private readonly IReaderRepository _readerRepository;
        private readonly IScumServerRepository _scumServerRepository;


        public FtpJob(
            IReaderRepository readerRepository,
            IFtpService ftpService,
            IScumServerRepository scumServerRepository,
            string baseFileName)
        {
            _ftpService = ftpService;
            _baseFileName = baseFileName;
            _readerRepository = readerRepository;
            _scumServerRepository = scumServerRepository;
        }

        private async Task SaveLine(Line line)
        {
            await _readerRepository.AddAsync(new Reader(line.File, line.Value, line.Hash));
            await _readerRepository.SaveAsync();
        }

        private long GetServerIdFromContext(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            long? serverId = dataMap.GetLong("server_id");

            if (!serverId.HasValue)
            {
                throw new Exception("No value for variable serverId");
            }

            return serverId.Value;
        }

        public IEnumerable<string> GetLogFiles(Ftp ftp)
        {
            var timeStampYesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            var timeStampToday = DateTime.Now.ToString("yyyyMMdd");
            var timeStampTomorrow = DateTime.Now.AddDays(1).ToString("yyyyMMdd");
            var client = _ftpService.GetClient(ftp);

            var files = client.GetNameListing(ftp.RootFolder);
            return files.ToList()
                .Where(fileName =>
                fileName.StartsWith(_baseFileName + timeStampYesterday) || fileName.StartsWith(_baseFileName + timeStampToday) || fileName.StartsWith(_baseFileName + timeStampTomorrow))
                .OrderBy(x => DateTime.ParseExact(x.Split("_")[1].Replace(".log", string.Empty), "yyyyMMddHHmmss", null));
        }

        public async Task<ScumServer> GetServerAsync(IJobExecutionContext context)
        {
            var serverId = GetServerIdFromContext(context);

            var server = await _scumServerRepository.FindByIdAsync(serverId);
            if (server?.Ftp is null)
            {
                throw new Exception("Invalid server: the server is non existent or does not have a ftp configuration");
            }

            return server;
        }

        public async Task<IList<Line>> GetUnreadFileLinesAsync(Ftp ftp, string fileName)
        {
            var readings = await _readerRepository.FindAsync(reader => reader.CreateDate.Date == DateTime.Now.Date);
            List<Line> lines = [];
            if (readings.Any())
            {
                lines = readings
                  .Select(reader => new Line(reader.Value, reader.FileName, reader.Hash))
                  .DistinctBy(x => x.Hash)
                  .ToList();
            }

            var client = _ftpService.GetClient(ftp);
            using (var stream = client.OpenRead(ftp.RootFolder + fileName, FtpDataType.ASCII))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var fixedLine = line.Replace("\0", "");
                    var lineObject = new Line(fixedLine, fileName);
                    if (!lines.Any(l => l.Hash.Equals(lineObject.Hash)))
                    {
                        lines.Add(lineObject);
                        SaveLine(lineObject);
                    }
                }

                return lines;
            }
        }
    }
}
