using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponse?> PreAuthenticate(AuthenticateDto authenticateDto);
        Task<AuthResponse?> Authenticate(long serverId);
        Task<UserDto> Register(RegisterUserDto register);
    }
}
