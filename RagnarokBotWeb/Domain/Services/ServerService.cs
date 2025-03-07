using AutoMapper;
using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Text;

namespace RagnarokBotWeb.Domain.Services
{
    public class ServerService : BaseService, IServerService
    {
        private readonly ILogger<ServerService> _logger;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFtpService _ftpService;
        private readonly IMapper _mapper;

        public ServerService(
            IHttpContextAccessor httpContext,
            ILogger<ServerService> logger,
            IScumServerRepository scumServerRepository,
            ITenantRepository tenantRepository,
            IUnitOfWork unitOfWork,
            IFtpService ftpService,
            IMapper mapper) : base(httpContext)
        {
            _logger = logger;
            _scumServerRepository = scumServerRepository;
            _tenantRepository = tenantRepository;
            _unitOfWork = unitOfWork;
            _ftpService = ftpService;
            _mapper = mapper;
        }

        public async Task<ScumServerDto> ChangeFtp(FtpDto ftpDto)
        {
            var tenantId = TenantId();
            if (!tenantId.HasValue) throw new UnauthorizedException("Invalid token");

            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid token");

            var tenant = await _tenantRepository.FindByIdAsync(tenantId.Value);
            if (tenant is null) throw new DomainException("Tenant not found");

            var server = await _scumServerRepository.FindByIdAsync(serverId.Value);
            if (server is null) throw new DomainException("ScumServer not found");

            var newFtp = new Ftp();
            newFtp.Address = ftpDto.Address;
            newFtp.Port = ftpDto.Port;
            newFtp.UserName = ftpDto.UserName;
            newFtp.Password = ftpDto.Password;
            newFtp.Provider = ftpDto.Provider;
            newFtp.RootFolder = newFtp.GetRootFolder();

            server.Name = GetServerConfigLineValue(newFtp, "ServerName");

            if (server.Ftp is not null)
            {
                _unitOfWork.Ftps.Remove(server.Ftp);
                await _unitOfWork.SaveAsync();
            }
            server.Ftp = newFtp;

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();

            return _mapper.Map<ScumServerDto>(server);
        }

        public string GetServerConfigLineValue(Ftp ftp, string config)
        {
            if (ftp is null) throw new DomainException("Invalid ftp server");
            try
            {
                var client = _ftpService.GetClient(ftp);
                using (var stream = client.OpenRead($"{ftp.RootFolder}Configs/ServerSettings.ini", FtpDataType.ASCII))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var fixedLine = line.Replace("\0", "");
                        if (fixedLine.Contains($"scum.{config}="))
                        {
                            return fixedLine.Split("=")[1];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new DomainException("Invalid FTP server");
            }
            return null;
        }

        public string UpdateConfigLine(Ftp ftp, string config, string newValue)
        {

            if (ftp is null) throw new DomainException("Invalid ftp server");

            try
            {
                string tempLocalPath = "temp_file.txt"; // Temporary local file
                string remoteFilePath = $"{ftp.RootFolder}/Configs/ServerSettings.ini";

                using (FtpClient client = _ftpService.GetClient(ftp))
                {
                    // Download file into memory stream
                    using (MemoryStream stream = new())
                    {
                        client.DownloadStream(stream, remoteFilePath);
                        stream.Position = 0; // Reset stream position

                        // Read all lines into an array
                        string[] lines = new StreamReader(stream).ReadToEnd().Split(Environment.NewLine);

                        // Find the index of the line containing the search text
                        int lineIndex = Array.FindIndex(lines, line => line.Contains(config));

                        if (lineIndex != -1)
                        {
                            lines[lineIndex] = $"{lines[lineIndex].Split("=")[0]}={newValue}"; // Replace line

                            // Convert back to a MemoryStream for uploading
                            string updatedContent = string.Join(Environment.NewLine, lines);
                            MemoryStream updatedStream = new(Encoding.UTF8.GetBytes(updatedContent));

                            // Upload modified content
                            client.UploadStream(updatedStream, remoteFilePath);
                            _logger.LogInformation("Updated line {} successfully with value {} for server {}", lines[lineIndex], newValue, ServerId()!);
                        }
                        else
                        {
                            Console.WriteLine("Text not found in the file.");
                        }
                    }
                    client.Disconnect();
                }
            }
            catch (Exception)
            {
                throw new DomainException("Invalid ftp server");
            }
            return null;
        }

        public async Task AddGuild(ChangeGuildDto guildDto)
        {
            var tenantId = TenantId();
            if (!tenantId.HasValue) throw new UnauthorizedException("Invalid token");

            var tenant = await _tenantRepository.FindByIdAsync(tenantId.Value);
            if (tenant is null) throw new DomainException("Tenant not found");

            var server = await _scumServerRepository.FindByIdAsync(tenantId.Value);
            if (server is null) throw new DomainException("ScumServer not found");

            if (server.Guild is not null) throw new DomainException("Server already has a guild");
            server.Guild = new Guild()
            {
                Enabled = true, // TODO: Verificar se pagamento está em dia
                DiscordId = guildDto.GuildId,
                RunTemplate = true,
            };

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();
        }

        public async Task<ScumServer> GetServer(long serverId)
        {
            var server = await _scumServerRepository.FindByIdAsync(serverId);
            if (server is null) throw new NotFoundException("Server not found");
            if (!server.Tenant.Enabled) throw new DomainException("Server tenant is not avaiable");
            return server;
        }
    }
}
