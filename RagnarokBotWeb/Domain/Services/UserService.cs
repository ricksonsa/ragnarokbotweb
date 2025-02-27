using Microsoft.IdentityModel.Tokens;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RagnarokBotWeb.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;

        public UserService(ILogger<UserService> logger, IUserRepository userRepository, ITenantRepository tenantRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
        }

        public async Task<TokenResult?> Authenticate(AuthenticateDto authenticateDto)
        {
            var user = await _userRepository.FindOneWithTenantAsync(u => u.Email == authenticateDto.Email);

            if (user is null) return null;
            if (!user.Active) return null;
            if (!user.IsTenantAvaiable()) return null;
            if (!PasswordHasher.VerifyPassword(authenticateDto.Password, user.PasswordHash, user.PasswordSalt)) return null;

            return new TokenResult
            {
                AccessToken = GenerateJwtToken(user),
            };
        }

        public async Task<UserDto> Register(RegisterUserDto register)
        {
            if (await _userRepository.HasAny(user => user.Email == register.Email))
            {
                throw new DomainException("Email already in use");
            }

            var user = new User
            {
                Email = register.Email,
                Active = true
            };

            user.SetPassword(register.Password);
            if (register.TenantId.HasValue)
            {
                user.Tenant = await _tenantRepository.FindByIdAsync(register.TenantId.Value);
            }

            await _userRepository.AddAsync(user);
            await _userRepository.SaveAsync();

            return new UserDto()
            {
                Email = register.Email
            };

        }

        private string GenerateJwtToken(User user)
        {
            var keyBytes = Encoding.UTF8.GetBytes("p2tfCQNn6FJrM7XmdAsW5zKc4DHyYbELwuPV93BRv8xeqkSjZa\r\nVhN64mSPatj9H5FqfU2rCTEWvpskKQy3eZwLGXnb8RudD7zBYM\r\nwRJXr2b6tsQZWNLUDV4C8nmpKyc7fagGqh5MFux39kASvPEdBz\r\nZd7wKDnsq8j9WTHaGmbAkeYN4RPJrEp3UXS5LCvQy6hzxBVMcF\r\nsDh3SGNHjf7qARkxzMe2VpyPncmbvJCKTX4ruZWtB86dLQ9YF5");
            var textKey = Convert.ToBase64String(keyBytes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(textKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(ClaimConstants.TenantId, user.Tenant?.Id.ToString() ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                issuer: "ragnarokbotowner",
                audience: "ragnarokbotowner",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
