using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordQuoteBot.Configuration;
using DiscordQuoteBot.Data;
using DiscordQuoteBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions()
    .Configure<DiscordBotConfiguration>(
        builder.Configuration.GetRequiredSection(DiscordBotConfiguration.ConfigurationKey))
    .AddSingleton(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.AllUnprivileged & GatewayIntents.GuildMembers &
                         ~GatewayIntents.GuildScheduledEvents &
                         ~GatewayIntents.GuildInvites
    })
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton(x =>
        new InteractionService(x.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig()))
    .AddSingleton<ServerDataService>()
    .AddDbContext<QuoteBotDbContext>(options =>
    {
        var sqliteConfiguration = builder.Configuration
            .GetRequiredSection(SqliteConfiguration.ConfigurationKey)
            .Get<SqliteConfiguration>();
        if (sqliteConfiguration is null)
        {
            throw new InvalidOperationException("Sqlite database file path not configured!");
        }

        options.UseSqlite($"Data Source={sqliteConfiguration.FilePath}");
    })
    .AddHostedService<LegacyDataFileConversionService>()
    .AddHostedService<DiscordBotService>()
    .AddHostedService<InteractionHandlerService>();

var serviceProvider = builder.Services.BuildServiceProvider();
builder.Services.AddSingleton(serviceProvider);

await serviceProvider.GetRequiredService<QuoteBotDbContext>().Database.MigrateAsync();

var host = builder.Build();

host.Run();
