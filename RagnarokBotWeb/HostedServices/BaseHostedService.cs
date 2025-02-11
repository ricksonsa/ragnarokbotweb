using FluentFTP;

namespace RagnarokBotWeb.HostedServices
{
    public abstract class BaseHostedService
    {
        private readonly FtpClient _ftpClient;
        private readonly string _baseFileName;
        private readonly Dictionary<string, int> _processedLines = [];

        public BaseHostedService(FtpClient ftpClient, string baseFileName)
        {
            _ftpClient = ftpClient;
            _baseFileName = baseFileName;
        }
        public abstract Task Process();

        public IEnumerable<string> GetLogFiles()
        {
            var timeStamp = DateTime.Now.ToString("yyyyMMdd");
            var timeStampToday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            var files = _ftpClient.GetNameListing("/189.1.169.132_7000/");
            return files.ToList().Where(fileName => fileName.StartsWith(_baseFileName + timeStamp) || fileName.StartsWith(_baseFileName + timeStampToday));
        }

        public IList<string> GetUnreadFileLines(string fileName)
        {
            using (var stream = _ftpClient.OpenRead("/189.1.169.132_7000/" + fileName, FtpDataType.ASCII))
            using (var reader = new StreamReader(stream))
            {
                string line;
                IList<string> lines = [];
                int counter = _processedLines.ContainsKey(fileName) ? _processedLines[fileName] : 0;

                // FIXME: Melhorar essa parada aqui
                // Para skipar as linhas ja lidas
                for (int i = 0; i < counter - 1; i++)
                {
                    reader.ReadLine();
                }

                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                    if (_processedLines.ContainsKey(fileName))
                    {
                        _processedLines[fileName] = counter;
                    }
                    else
                    {
                        _processedLines.Add(fileName, counter);
                    }
                    counter++;
                }

                return lines;
            }
        }
    }
}
