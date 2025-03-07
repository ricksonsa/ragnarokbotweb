using AutoMapper;
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
        private readonly IMapper _mapper;

        public UserService(IHttpContextAccessor contextAccessor,
            ILogger<UserService> logger,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IScumServerRepository scumServerRepository,
            ITenantRepository tenantRepository,
            ITokenIssuer tokenIssuer,
            IMapper mapper) : base(contextAccessor)
        {
            _logger = logger;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _scumServerRepository = scumServerRepository;
            _tenantRepository = tenantRepository;
            _tokenIssuer = tokenIssuer;
            _mapper = mapper;
        }

        public async Task<AuthResponse?> PreAuthenticate(AuthenticateDto authenticateDto)
        {
            var user = await _userRepository.FindOneWithTenantAsync(u => u.Email == authenticateDto.Email);

            if (user is null) throw new UnauthorizedException("User not registered");
            if (!user.Active) throw new UnauthorizedException("User not active");
            if (!user.IsTenantAvaiable()) throw new UnauthorizedException("User not active");
            if (!PasswordHasher.VerifyPassword(authenticateDto.Password, user.PasswordHash, user.PasswordSalt)) throw new UnauthorizedException("Invalid login or password");
            var scumServers = await _scumServerRepository.FindManyByTenantIdAsync(user.Tenant.Id);

            return new AuthResponse
            {
                IdToken = _tokenIssuer.GenerateIdToken(user),
                ScumServers = scumServers.Select(_mapper.Map<ScumServerDto>)
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

        public async Task<AccountDto> GetAccount()
        {
            var scumServers = await _scumServerRepository.FindManyByTenantIdAsync(TenantId()!.Value);
            var serverId = ServerId()!.Value;
            return new AccountDto
            {
                Email = UserName()!,
                ServerId = serverId,
                Server = _mapper.Map<ScumServerDto>(scumServers.FirstOrDefault(server => server.Id == serverId)),
                Servers = scumServers.Select(_mapper.Map<ScumServerDto>),
            };
        }
    }
}
