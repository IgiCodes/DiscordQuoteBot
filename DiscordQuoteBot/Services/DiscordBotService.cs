using Discord;
using Discord.WebSocket;
using DiscordQuoteBot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DiscordQuoteBot.Services;

public class DiscordBotService(DiscordSocketClient client, IOptions<DiscordBotConfiguration> discordBotConfiguration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        client.Log += LogAsync;
        client.LoggedIn += async () => await LogAsync(LogSeverity.Info, "Client logged in!");
        client.LoggedOut += async () => await LogAsync(LogSeverity.Info, "Client logged out!");

        await client.LoginAsync(TokenType.Bot, discordBotConfiguration.Value.Token);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private static async Task LogAsync(LogSeverity severity, string message)
    {
        await LogAsync(new LogMessage(severity, nameof(DiscordBotService), message));
    }

    private static Task LogAsync(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
