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
        private readonly FastspringService _fastspringService;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenIssuer _tokenIssuer;
        private readonly ITaskService _taskService;
        private readonly IMapper _mapper;

        public UserService(IHttpContextAccessor contextAccessor,
            ILogger<UserService> logger,
            IUserRepository userRepository,
            IScumServerRepository scumServerRepository,
            ITenantRepository tenantRepository,
            ITokenIssuer tokenIssuer,
            IMapper mapper,
            ITaskService taskService,
            FastspringService fastspringService) : base(contextAccessor)
        {
            _logger = logger;
            _userRepository = userRepository;
            _scumServerRepository = scumServerRepository;
            _tenantRepository = tenantRepository;
            _tokenIssuer = tokenIssuer;
            _mapper = mapper;
            _taskService = taskService;
            _fastspringService = fastspringService;
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
            var user = await _userRepository.FindOneWithTenantAsync(u => u.Email == UserLogin());

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
                Name = register.Name,
                LastName = register.LastName,
                Email = register.Email,
                Country = register.Country,
                Active = true
            };

            user.SetPassword(register.Password);
            user.Tenant = await CreateTenant(user.Email);
            await CreateServer(user.Tenant);

            await _userRepository.CreateOrUpdateAsync(user);
            await _userRepository.SaveAsync();

            try
            {
                var account = await _fastspringService.CreateAccount(user);

                user.FastspringAccountId = account!.Account;

                await _userRepository.CreateOrUpdateAsync(user);
                await _userRepository.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register User Exception");
            }

            return new UserDto()
            {
                Email = register.Email
            };
        }

        public async Task<AccountDto> UpdateAccount(UserDto userDto)
        {
            var user = await _userRepository.FindOneAsync(user => user.Email == UserLogin());

            if (user is null) throw new NotFoundException("User not found");

            if (!string.IsNullOrEmpty(userDto.Password))
                user.SetPassword(userDto.Password);

            user.Email = userDto.Email;
            user.LastName = userDto.LastName;
            user.Name = userDto.Name;
            user.Country = userDto.Country;

            await _userRepository.CreateOrUpdateAsync(user);
            await _userRepository.SaveAsync();

            try
            {
                if (user.FastspringAccountId != null)
                {
                    var account = await _fastspringService.UpdateAccount(user);
                }
                else
                {
                    var account = await _fastspringService.CreateAccount(user);
                    user.FastspringAccountId = account!.Account;
                    await _userRepository.CreateOrUpdateAsync(user);
                    await _userRepository.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update User Exception");
            }

            return _mapper.Map<AccountDto>(user);
        }


        public async Task<ScumServer> CreateServer(Tenant tenant)
        {
            var server = new ScumServer(tenant);
            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();
            _taskService.NewServerAddedAsync(server);
            return server;
        }

        public async Task<Tenant> CreateTenant(string name)
        {
            var tenant = new Tenant
            {
                Enabled = true,
                Name = name,
            };

            await _tenantRepository.CreateOrUpdateAsync(tenant);
            await _tenantRepository.SaveAsync();

            return tenant;
        }

        public async Task<AccountDto> GetAccount()
        {
            var scumServers = await _scumServerRepository.FindManyByTenantIdAsync(TenantId()!.Value);
            var user = await _userRepository.FindOneAsync(u => u.Email == UserLogin()!);
            if (user is null) throw new UnauthorizedException("Invalid user!");

            var serverId = ServerId()!.Value;
            return new AccountDto
            {
                Name = user.Name,
                LastName = user.LastName,
                Email = UserLogin()!,
                Country = user.Country,
                ServerId = serverId,
                Server = _mapper.Map<ScumServerDto>(scumServers.FirstOrDefault(server => server.Id == serverId)),
                Servers = scumServers.Select(_mapper.Map<ScumServerDto>),
                AccessLevel = user.AccessLevel
            };
        }

        public Task<AccountDto?> UpdateAccount(AccountDto accountDto)
        {
            throw new NotImplementedException();
        }
    }
}
