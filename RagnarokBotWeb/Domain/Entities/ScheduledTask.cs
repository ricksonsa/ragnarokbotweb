using System.ComponentModel.DataAnnotations.Schema;

namespace RagnarokBotWeb.Domain.Entities
{
    public class ScheduledTask : BaseEntity
    {
        [Column("Commands")]
        private string _commands;

        [NotMapped]
        public List<string> Commands { get => _commands.Split(";").ToList(); set => _commands = string.Join(";", value); }

        public string Name { get; set; }
        public string CronExpression { get; set; }
        public ScumServer ScumServer { get; set; }
        public bool IsActive { get; set; }
    }
}
