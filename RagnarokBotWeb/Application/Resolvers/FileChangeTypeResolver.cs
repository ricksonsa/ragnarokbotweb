using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Application.Resolvers
{
    public class FileChangeTypeResolver
    {
        public string Resolve(EFileChangeType changeType)
        {
            switch (changeType)
            {
                case EFileChangeType.ServerSettings: return "";
                case EFileChangeType.SilencedUsers: return "";
                case EFileChangeType.BannedUsers: return "";
                case EFileChangeType.Whitelist: return "";
                default: return "";
            }
        }
    }
}
