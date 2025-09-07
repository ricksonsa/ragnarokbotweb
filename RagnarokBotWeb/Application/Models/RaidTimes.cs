using Newtonsoft.Json;
using RagnarokBotWeb.Domain.Entities;
using System.Globalization;

namespace RagnarokBotWeb.Application.Models
{
    public class RaidingTime
    {
        [JsonProperty("day", NullValueHandling = NullValueHandling.Ignore)]
        public string Day { get; set; }

        [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
        public string Time { get; set; }

        [JsonProperty("start-announcement-time", NullValueHandling = NullValueHandling.Ignore)]
        public string StartAnnouncementTime { get; set; }

        [JsonProperty("end-announcement-time", NullValueHandling = NullValueHandling.Ignore)]
        public string EndAnnouncementTime { get; set; }
    }

    public class RaidTimes
    {
        [JsonProperty("raiding-times", NullValueHandling = NullValueHandling.Ignore)]
        public List<RaidingTime> RaidingTimes { get; set; }

        private static bool IsBetweenDays(DayOfWeek start, DayOfWeek end, DayOfWeek current)
        {
            if (start <= end)
                return current >= start && current <= end;
            else // Wrap around the week
                return current >= start || current <= end;
        }

        private static HashSet<DayOfWeek> ExpandDays(string dayString)
        {
            var result = new HashSet<DayOfWeek>();

            if (dayString.Equals("Weekdays", StringComparison.OrdinalIgnoreCase))
            {
                result.UnionWith(new[] {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                });
            }
            else if (dayString.Equals("Weekend", StringComparison.OrdinalIgnoreCase))
            {
                result.UnionWith(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
            }
            else if (dayString.Contains("-"))
            {
                var parts = dayString.Split('-');
                var init = parts[0];
                var end = parts[1];
                if (end.StartsWith("00")) end = "23:59";
                if (Enum.TryParse(init, true, out DayOfWeek startDay) &&
                    Enum.TryParse(end, true, out DayOfWeek endDay))
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var current = (DayOfWeek)i;
                        if (IsBetweenDays(startDay, endDay, current))
                            result.Add(current);
                    }
                }
            }
            else
            {
                foreach (var part in dayString.Split(','))
                {
                    if (Enum.TryParse(part.Trim(), true, out DayOfWeek day))
                        result.Add(day);
                }
            }

            return result;
        }

        private bool IsRtInRaidingTime(RaidingTime rt, DateTimeOffset now)
        {
            var days = ExpandDays(rt.Day);
            if (!days.Contains(now.DayOfWeek)) return false;

            var times = rt.Time.Split('-');
            if (times.Length != 2) return false;

            var startStr = times[0].Trim();
            var endStr = times[1].Trim();

            if (!TimeSpan.TryParseExact(startStr, "hh\\:mm", CultureInfo.InvariantCulture, out var start) &&
                !TimeSpan.TryParseExact(startStr, "HH\\:mm", CultureInfo.InvariantCulture, out start))
            {
                return false; // invalid start format
            }

            if (!TimeSpan.TryParseExact(endStr, "hh\\:mm", CultureInfo.InvariantCulture, out var end) &&
                !TimeSpan.TryParseExact(endStr, "HH\\:mm", CultureInfo.InvariantCulture, out end))
            {
                return false; // invalid end format
            }

            // Treat 00:00 as midnight (end of day)
            if (end == TimeSpan.Zero)
                end = TimeSpan.FromDays(1);

            var nowTime = now.TimeOfDay;

            if (start <= end)
                return nowTime >= start && nowTime <= end;
            else
                return nowTime >= start || nowTime <= end; // overnight
        }


        public bool IsInRaidTime(ScumServer server)
        {
            var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, server.GetTimeZoneOrDefault());
            return RaidingTimes.Any(rt => IsRtInRaidingTime(rt, now));
        }
    }
}
