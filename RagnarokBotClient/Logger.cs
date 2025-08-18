using System.Reflection;

namespace RagnarokBotClient
{
    public static class Logger
    {
        public static event EventHandler<string> OnLogging;
        private static string m_exePath = string.Empty;

        public static void LogWrite(string logMessage, bool write = false)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (!File.Exists(m_exePath + "\\" + "log.txt"))
            {
                File.Create(m_exePath + "\\" + "log.txt");
            }

            try
            {
                if (write)
                {
                    using (StreamWriter w = File.AppendText(m_exePath + "\\" + "log.txt"))
                    {
                        AppendLog(logMessage, w);
                    }
                }
                OnLogging?.Invoke(null, logMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static void AppendLog(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.WriteLine("{0} {1}: {2}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString(), logMessage);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
