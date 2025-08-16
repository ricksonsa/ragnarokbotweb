namespace RagnarokBotWeb.Crosscutting.Utils
{
    using System;

    public static class CronConverter
    {
        /// <summary>
        /// Converts a standard 5-field cron expression (Linux style) to a Quartz.NET cron expression.
        /// </summary>
        /// <param name="standardCron">Standard cron expression (e.g. "30 5 * * *")</param>
        /// <returns>Quartz.NET cron expression (e.g. "0 30 5 * * ?")</returns>
        public static string ToQuartzCron(string standardCron)
        {
            if (string.IsNullOrWhiteSpace(standardCron))
                throw new ArgumentException("Cron expression cannot be null or empty", nameof(standardCron));

            var parts = standardCron.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 5)
                throw new ArgumentException("Standard cron must have exactly 5 fields (min hour day month dayOfWeek)", nameof(standardCron));

            string minute = parts[0];
            string hour = parts[1];
            string dayOfMonth = parts[2];
            string month = parts[3];
            string dayOfWeek = parts[4];

            // Convert day-of-week from 0–6 (Sun=0) → Quartz 1–7 (Sun=1)
            if (dayOfWeek != "*" && dayOfWeek != "?")
            {
                dayOfWeek = ReplaceDayOfWeek(dayOfWeek);
            }
            else
            {
                // Quartz requires ? in either dayOfMonth or dayOfWeek
                if (dayOfWeek == "*")
                    dayOfWeek = "?";
            }

            // Insert seconds = 0
            string quartzCron = $"0 {minute} {hour} {dayOfMonth} {month} {dayOfWeek}";

            return quartzCron;
        }

        private static string ReplaceDayOfWeek(string input)
        {
            // Handles single values and ranges/lists (e.g. "1-5", "1,3,5")
            string[] tokens = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].Contains('-'))
                {
                    var range = tokens[i].Split('-');
                    tokens[i] = $"{ShiftDay(range[0])}-{ShiftDay(range[1])}";
                }
                else
                {
                    tokens[i] = ShiftDay(tokens[i]);
                }
            }
            return string.Join(',', tokens);
        }

        private static string ShiftDay(string day)
        {
            if (day == "*" || day == "?") return day;

            if (int.TryParse(day, out int num))
            {
                // Standard: 0=Sun → Quartz: 1=Sun
                return num == 0 ? "1" : (num + 1).ToString();
            }
            return day;
        }
    }

}
