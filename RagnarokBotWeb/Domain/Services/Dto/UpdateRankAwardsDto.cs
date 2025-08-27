namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class UpdateRankAwardsDto
    {
        public long? KillRankMonthlyTop1Award { get; set; }
        public long? KillRankMonthlyTop2Award { get; set; }
        public long? KillRankMonthlyTop3Award { get; set; }
        public long? KillRankMonthlyTop4Award { get; set; }
        public long? KillRankMonthlyTop5Award { get; set; }

        public long? KillRankWeeklyTop1Award { get; set; }
        public long? KillRankWeeklyTop2Award { get; set; }
        public long? KillRankWeeklyTop3Award { get; set; }
        public long? KillRankWeeklyTop4Award { get; set; }
        public long? KillRankWeeklyTop5Award { get; set; }

        public long? KillRankDailyTop1Award { get; set; }
        public long? KillRankDailyTop2Award { get; set; }
        public long? KillRankDailyTop3Award { get; set; }
        public long? KillRankDailyTop4Award { get; set; }
        public long? KillRankDailyTop5Award { get; set; }

        public long? LockpickRankDailyTop1Award { get; set; }
        public long? LockpickRankDailyTop2Award { get; set; }
        public long? LockpickRankDailyTop3Award { get; set; }
        public long? LockpickRankDailyTop4Award { get; set; }
        public long? LockpickRankDailyTop5Award { get; set; }
    }
}
