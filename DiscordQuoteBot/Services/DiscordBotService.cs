using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordQuoteBot.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DiscordQuoteBot.Services;

public class DiscordBotService(
    IHostEnvironment environment,
    ServiceProvider serviceProvider,
    DiscordSocketClient client,
    IOptions<DiscordBotConfiguration> discordBotConfiguration) : BackgroundService
{
    private readonly InteractionService _interactionService = new(client.Rest);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        client.Log += Log;
        client.Ready += async () =>
        {
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

            var testGuildId = discordBotConfiguration.Value.TestGuildId;
            if (environment.IsDevelopment() && testGuildId is not null)
            {
                Console.WriteLine($"In development environment, adding commands to {testGuildId}...");
                await _interactionService.RegisterCommandsToGuildAsync(testGuildId.Value);
            }
            else
            {
                await _interactionService.RegisterCommandsGloballyAsync();
            }

            client.InteractionCreated += async interaction =>
            {
                var scope = serviceProvider.CreateScope();
                var ctx = new SocketInteractionContext(client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, scope.ServiceProvider);
            };
        };

        await client.LoginAsync(TokenType.Bot, discordBotConfiguration.Value.Token);
        await client.StartAsync();
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
