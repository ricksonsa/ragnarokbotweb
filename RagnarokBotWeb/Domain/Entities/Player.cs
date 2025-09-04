using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities;
public class Player : BaseEntity
{
    public string? Name { get; set; }
    public string? ScumId { get; set; }
    public string? SteamId64 { get; set; }
    public string? SteamName { get; set; }
    public ulong? DiscordId { get; set; }
    public string? DiscordName { get; set; }
    public long ScumServerId { get; set; }           // FK
    public ScumServer ScumServer { get; set; }
    public bool HideAdminCommands { get; set; }
    public long? Money { get; set; }
    public long? Gold { get; set; }
    public long? Fame { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Z { get; set; }
    public long Coin { get; set; } = 0;
    public List<Vip> Vips { get; set; }
    public List<Ban> Bans { get; set; }
    public List<Silence> Silences { get; set; }
    public List<DiscordRole> DiscordRoles { get; set; }
    public DateTime? LastLoggedIn { get; set; }
    public string? IpAddress { get; set; }

    public bool IsVip() => Vips?.Any(vip => vip.Indefinitely || vip.ExpirationDate.HasValue && vip.ExpirationDate.Value.Date > DateTime.UtcNow.Date && !vip.Processed) ?? false;
    public bool IsSilenced() => Silences?.Any(silence => silence.Indefinitely || silence.ExpirationDate.HasValue && silence.ExpirationDate.Value.Date > DateTime.UtcNow.Date && !silence.Processed) ?? false;
    public bool IsBanned() => Bans?.Any(ban => ban.Indefinitely || !ban.Processed && ban.ExpirationDate.HasValue && ban.ExpirationDate.Value.Date > DateTime.UtcNow.Date) ?? false;

    public Vip? RemoveVip()
    {
        return Vips?.FirstOrDefault(vip => vip.Indefinitely || !vip.Processed);
    }

    public Silence? RemoveSilence()
    {

        return Silences?.FirstOrDefault(silence => silence.Indefinitely || !silence.Processed);
    }

    public Ban? RemoveBan()
    {
        return Bans?.FirstOrDefault(ban => ban.Indefinitely || !ban.Processed);
    }


    public Vip AddVip(int? days, ulong? discordRoleId = null)
    {
        Vips ??= [];
        if (days.HasValue && days.Value > 0)
        {
            var date = DateTime.UtcNow;
            var vip = new Vip(date.AddDays(days.Value));
            vip.DiscordRoleId = discordRoleId;
            Vips.Add(vip);
            return vip;
        }
        else
        {
            var vip = new Vip();
            vip.DiscordRoleId = discordRoleId;
            Vips.Add(vip);
            return vip;
        }
    }

    public void AddSilence(int? days)
    {
        Silences ??= [];
        if (days.HasValue && days.Value > 0)
        {
            var date = DateTime.UtcNow;
            Silences.Add(new Silence(date.AddDays(days.Value)));
        }
        else
        {
            Silences.Add(new Silence());
        }
    }

    public void AddBan(int? days)
    {
        Bans ??= [];
        if (days.HasValue && days.Value > 0)
        {
            var date = DateTime.UtcNow;
            Bans.Add(new Ban(date.AddDays(days.Value)));
        }
        else
        {
            Bans.Add(new Ban());
        }
    }

    public void AddDiscordRole(int? days, ulong discordId)
    {
        DiscordRoles ??= [];

        if (DiscordRoles.Any(role => role.DiscordId == discordId))
            return;

        if (days.HasValue && days.Value > 0)
        {
            var date = DateTime.UtcNow;
            DiscordRoles.Add(new DiscordRole(date.AddDays(days.Value)) { DiscordId = discordId });
        }
        else
        {
            DiscordRoles.Add(new DiscordRole() { DiscordId = discordId });
        }
    }

}