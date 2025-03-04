namespace RagnarokBotWeb.Domain.Enums;

public enum EPlayerStatus
{
    /// <summary>
    ///     When the player is created but not registered.
    ///     This occurs when ScumServer saves Player events.
    /// </summary>
    Unregistered = 0,

    /// <summary>
    ///     When the player requests a "welcome pack" on Discord and receives a code to put into the game.
    /// </summary>
    Registering = 1,

    /// <summary>
    ///     When the player completes "welcome pack" process.
    /// </summary>
    Active = 2,

    /// <summary>
    ///     When Admin remove the player.
    /// </summary>
    Inactive = 3
}