using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class UserService : BaseService, IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenIssuer _tokenIssuer;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IHttpContextAccessor contextAccessor,
            ILogger<UserService> logger,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IScumServerRepository scumServerRepository,
            ITenantRepository tenantRepository,
            ITokenIssuer tokenIssuer) : base(contextAccessor)
        {
            _logger = logger;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _scumServerRepository = scumServerRepository;
            _tenantRepository = tenantRepository;
            _tokenIssuer = tokenIssuer;
        }

        public async Task<AuthResponse?> PreAuthenticate(AuthenticateDto authenticateDto)
        {
            var user = await _userRepository.FindOneWithTenantAsync(u => u.Email == authenticateDto.Email);

            if (user is null) throw new UnauthorizedException("User not registered");
            if (!user.Active) throw new UnauthorizedException("User not active");
            if (!user.IsTenantAvaiable()) throw new UnauthorizedException("User not active");
            if (!PasswordHasher.VerifyPassword(authenticateDto.Password, user.PasswordHash, user.PasswordSalt)) throw new UnauthorizedException("Invalid login or password");
            var scumServers = await _scumServerRepository.FindByTenantIdAsync(user.Tenant.Id);

            return new AuthResponse
            {
                IdToken = _tokenIssuer.GenerateIdToken(user),
                ScumServers = scumServers
            };
        }

        public async Task<AuthResponse?> Authenticate(long serverId)
        {
            var user = await _userRepository.FindOneWithTenantAsync(u => u.Email == UserName());

            if (user is null) throw new UnauthorizedException("User not registered");
            if (!user.Active) throw new UnauthorizedException("User not active");
            if (!user.IsTenantAvaiable()) throw new UnauthorizedException("User not active");

            return new AuthResponse(_tokenIssuer.GenerateAccessToken(user, serverId));
        }

        public async Task<UserDto> Register(RegisterUserDto register)
        {
            if (await _userRepository.HasAny(user => user.Email == register.Email))
                throw new DomainException("Email already in use");

            var user = new User
            {
                Email = register.Email,
                Active = true
            };
            user.SetPassword(register.Password);

            if (register.TenantId.HasValue)
            {
                var tenant = await _tenantRepository.FindByIdAsync(register.TenantId.Value);
                if (tenant is null) throw new DomainException("Tenant not found");
                user.Tenant = tenant;
            }
            else
            {
                user.Tenant = await CreateTenant(user.Email);
                await CreateServer(user.Tenant);
            }

            await _userRepository.AddAsync(user);
            await _userRepository.SaveAsync();

            return new UserDto()
            {
                Email = register.Email
            };
        }

        public async Task CreateServer(Tenant tenant)
        {
            await _scumServerRepository.CreateOrUpdateAsync(new ScumServer(tenant));
            await _scumServerRepository.SaveAsync();
        }

        public async Task ChangeFtp(FtpDto ftpDto)
        {
            var tenantId = TenantId();
            if (!tenantId.HasValue) throw new UnauthorizedException("Invalid token");

            var tenant = await _tenantRepository.FindByIdAsync(tenantId.Value);
            if (tenant is null) throw new DomainException("Tenant not found");

            var server = await _scumServerRepository.FindByIdAsync(tenantId.Value);
            if (server is null) throw new DomainException("ScumServer not found");

            if (server.Ftp is not null)
            {
                _unitOfWork.Ftps.Remove(server.Ftp);
                server.Ftp = null;
                await _unitOfWork.SaveAsync();
            }

            server.Ftp = new Ftp
            {
                Address = ftpDto.IpAddress,
                Port = ftpDto.Port,
                UserName = ftpDto.UserName,
                Password = ftpDto.Password
            };

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();
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

        public async Task<Tenant> CreateTenant(string name)
        {
            var tenant = new Tenant
            {
                Enabled = true,
                Name = name,
            };

            await _tenantRepository.CreateOrUpdateAsync(tenant);
            await _unitOfWork.SaveAsync();

            return tenant;
        }



    }
}
