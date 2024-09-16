namespace DiscordQuoteBot.Configuration;

public class DiscordBotConfiguration
{
    public const string ConfigurationKey = "DiscordBot";

    public required string Token { get; init; }

    public ulong? TestGuildId { get; init; }
}
