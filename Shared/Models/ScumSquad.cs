namespace Shared.Models
{
    public class ScumSquad
    {
        public int SquadId { get; set; }
        public string SquadName { get; set; }
        public List<SquadMember> Members { get; set; } = new();
    }
}
