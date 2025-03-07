namespace RagnarokBotWeb.Domain.Enums;

public enum EPlayerRegisterStatus
{
    /// <summary>
    ///     When the player requests a "welcome pack" on Discord and receives a code to put into the game.
    /// </summary>
    Registering = 0,

    /// <summary>
    ///     When the player completes "welcome pack" process.
    /// </summary>
    Registered = 1
}