using Discord;
using Discord.WebSocket;
using DiscordQuoteBot.Configuration;
using DiscordQuoteBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions()
    .Configure<DiscordBotConfiguration>(builder.Configuration.GetRequiredSection(DiscordBotConfiguration.ConfigurationKey))
    .AddSingleton(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents &
                         ~GatewayIntents.GuildInvites
    })
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton<ServerDataService>()
    .AddHostedService<LegacyDataFileConversionService>()
    .AddHostedService<DiscordBotService>();

var serviceProvider = builder.Services.BuildServiceProvider();
builder.Services.AddSingleton(serviceProvider);

var host = builder.Build();
host.Run();
