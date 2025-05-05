using RagnarokBotWeb.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagnarokBotWeb.Domain.Entities
{
    public class ScheduledTask : BaseEntity
    {
        [Column("Commands")]
        private string _commands;
        [NotMapped]
        public List<string> Commands { get => _commands.Split(";").ToList(); set => _commands = string.Join(";", value); }

        [Column("Conditions")]
        private string _conditions;
        [NotMapped]
        public List<string> Conditions { get => _conditions.Split(";").ToList(); set => _conditions = string.Join(";", value); }

        public EScheduledTaskType ScheduledTaskType { get; set; } = EScheduledTaskType.Commands;
        public string? Key { get; set; } // In case it being a ServerSettings ScheduledTaskType
        public string? Value { get; set; } // In case it being a ServerSettings ScheduledTaskType
        public string Name { get; set; }
        public string? Description { get; set; }
        public string CronExpression { get; set; }
        public ScumServer ScumServer { get; set; }
        public bool BlockedRaidTimes { get; set; }
        public bool IsActive { get; set; }
    }
}
