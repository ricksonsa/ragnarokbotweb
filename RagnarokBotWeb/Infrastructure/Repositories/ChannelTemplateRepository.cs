using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class ChannelTemplateRepository(AppDbContext context)
    : Repository<ChannelTemplate>(context), IChannelTemplateRepository;