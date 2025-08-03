namespace Shared.Models
{
    public class Squad
    {
        public int SquadId { get; set; }
        public string SquadName { get; set; }
        public List<SquadMember> Members { get; set; } = new();
    }
}
