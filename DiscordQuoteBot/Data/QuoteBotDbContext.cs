using DiscordQuoteBot.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DiscordQuoteBot.Data;

public class QuoteBotDbContext(DbContextOptions<QuoteBotDbContext> options) : DbContext(options)
{
    public DbSet<Quote> Quotes { get; [UsedImplicitly] set; }
}
