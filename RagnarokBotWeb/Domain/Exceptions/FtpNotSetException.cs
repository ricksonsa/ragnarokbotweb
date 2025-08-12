namespace RagnarokBotWeb.Domain.Exceptions
{
    public class FtpNotSetException() : Exception("Server does not have a FTP configuration")
    {
    }
}
