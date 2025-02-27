using RagnarokBotWeb.Domain.Services.Dto;
using Shared.Security;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IUserService
    {
        Task<TokenResult?> Authenticate(AuthenticateDto authenticateDto);
        Task<UserDto> Register(RegisterUserDto register);
    }
}
