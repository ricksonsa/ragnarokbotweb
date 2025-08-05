namespace RagnarokBotWeb.Application.Models
{
    public class ClientLocation
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Killer
    {
        public ServerLocation ServerLocation { get; set; }
        public ClientLocation ClientLocation { get; set; }
        public bool IsInGameEvent { get; set; }
        public string ProfileName { get; set; }
        public string UserId { get; set; }
        public bool HasImmortality { get; set; }
    }

    public class PreParseKill
    {
        public DateTime Date { get; set; }
        public double Distance { get; set; }
        public Killer Killer { get; set; }
        public Victim Victim { get; set; }
        public string Weapon { get; set; }
        public string TimeOfDay { get; set; }
        public string Line { get; internal set; }
    }

    public class ServerLocation
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Victim
    {
        public ServerLocation ServerLocation { get; set; }
        public ClientLocation ClientLocation { get; set; }
        public bool IsInGameEvent { get; set; }
        public string ProfileName { get; set; }
        public string UserId { get; set; }
    }


}
