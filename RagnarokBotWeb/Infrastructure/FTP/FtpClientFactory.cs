using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using System.Net.Security;

namespace RagnarokBotWeb.Infrastructure.FTP;

public static class FtpClientFactory
{
    public static AsyncFtpClient CreateClient(Ftp ftp, bool acceptInvalidCertificates = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ftp.Address) || string.IsNullOrWhiteSpace(ftp.UserName))
            throw new ArgumentException("FTP configuration is invalid.");

        var client = new AsyncFtpClient(ftp.Address, port: (int)ftp.Port, user: ftp.UserName, pass: ftp.Password)
        {
            Config = new FtpConfig
            {
                LogHost = false,
                LogDurations = false,
                LogPassword = false,
                LogUserName = false,
                LogToConsole = true,
                RetryAttempts = 5,
                EncryptionMode = FtpEncryptionMode.Auto,
                ConnectTimeout = 10000,
                DataConnectionType = FtpDataConnectionType.AutoPassive,
                ValidateAnyCertificate = acceptInvalidCertificates
            }
        };

        client.AutoConnect(cancellationToken);

        // Custom certificate validation if needed
        if (!acceptInvalidCertificates)
        {
            client.ValidateCertificate += (control, e) =>
            {
                // Log certificate details for debugging
                System.Diagnostics.Debug.WriteLine($"Certificate Subject: {e.Certificate?.Subject}");
                System.Diagnostics.Debug.WriteLine($"Certificate Issuer: {e.Certificate?.Issuer}");
                System.Diagnostics.Debug.WriteLine($"Policy Errors: {string.Join(", ", e.PolicyErrors)}");

                // Custom validation logic here
                // For now, reject invalid certificates (default behavior)
                e.Accept = false;
            };
        }

        return client;
    }

    // Alternative method for development/testing with relaxed SSL validation
    public static AsyncFtpClient CreateClientForDevelopment(Ftp ftp)
    {
        return CreateClient(ftp, acceptInvalidCertificates: true);
    }

    // Method with explicit SSL configuration
    public static FtpClient CreateClientWithSslConfig(Ftp ftp, FtpEncryptionMode encryptionMode, bool validateCertificate = true)
    {
        if (string.IsNullOrWhiteSpace(ftp.Address) || string.IsNullOrWhiteSpace(ftp.UserName))
            throw new ArgumentException("FTP configuration is invalid.");

        var client = new FtpClient(ftp.Address, port: (int)ftp.Port, user: ftp.UserName, pass: ftp.Password)
        {
            Config = new FtpConfig
            {
                LogHost = false,
                LogDurations = false,
                LogPassword = false,
                LogUserName = false,
                LogToConsole = false,
                EncryptionMode = encryptionMode,
                ConnectTimeout = 5000,
                DataConnectionType = FtpDataConnectionType.AutoPassive,
                ValidateAnyCertificate = !validateCertificate,
                // Additional SSL/TLS settings
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            }
        };

        if (validateCertificate)
        {
            client.ValidateCertificate += Client_ValidateCertificate; ;
        }

        return client;
    }

    private static void Client_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
    {
        // Log certificate information for debugging
        System.Diagnostics.Debug.WriteLine($"Validating certificate for: {control.Host}");
        System.Diagnostics.Debug.WriteLine($"Certificate Subject: {e.Certificate?.Subject}");
        System.Diagnostics.Debug.WriteLine($"Certificate Issuer: {e.Certificate?.Issuer}");
        System.Diagnostics.Debug.WriteLine($"Policy Errors: {string.Join(", ", e.PolicyErrors)}");

        // Custom validation rules
        if (e.PolicyErrors.HasFlag(SslPolicyErrors.None))
        {
            e.Accept = true;
            return;
        }

        // Allow specific common issues in development/internal environments
        var acceptableErrors = SslPolicyErrors.RemoteCertificateNameMismatch |
                              SslPolicyErrors.RemoteCertificateChainErrors;

        if ((e.PolicyErrors & ~acceptableErrors) == SslPolicyErrors.None)
        {
            // Only name mismatch or chain errors - might be acceptable for internal servers
            System.Diagnostics.Debug.WriteLine("Certificate has acceptable errors for internal use");
            // e.Accept = true; // Uncomment if you want to accept these errors
        }

        // For production, be strict about certificates
        e.Accept = false;
    }
}