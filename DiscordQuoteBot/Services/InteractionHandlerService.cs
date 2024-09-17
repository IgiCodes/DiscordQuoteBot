using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordQuoteBot.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DiscordQuoteBot.Services;

public class InteractionHandlerService(
    IHostEnvironment environment,
    ServiceProvider serviceProvider,
    DiscordSocketClient client,
    InteractionService interactionService,
    IOptions<DiscordBotConfiguration> discordBotConfiguration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        client.Ready += ReadyAsync;
        interactionService.Log += LogAsync;

        await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

        // Process the InteractionCreated payloads to execute Interactions commands
        client.InteractionCreated += HandleInteraction;

        // Also process the result of the command execution.
        interactionService.InteractionExecuted += HandleInteractionExecute;
    }

    private async Task ReadyAsync()
    {
        var testGuildId = discordBotConfiguration.Value.TestGuildId;
        if (environment.IsDevelopment() && testGuildId is not null)
        {
            await LogAsync(new LogMessage(LogSeverity.Info, nameof(DiscordBotService), $"In development environment, adding commands to {testGuildId}..."));
            await interactionService.RegisterCommandsToGuildAsync(testGuildId.Value);
        }
        else
        {
            await interactionService.RegisterCommandsGloballyAsync();
        }
    }

    private static Task LogAsync(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            var context = new SocketInteractionContext(client, interaction);

            // Execute the incoming command.
            var result = await interactionService.ExecuteCommandAsync(context, serviceProvider);

            // Due to async nature of InteractionFramework, the result here may always be success.
            // That's why we also need to handle the InteractionExecuted event.
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case null:
                    case InteractionCommandError.UnknownCommand:
                    case InteractionCommandError.ConvertFailed:
                    case InteractionCommandError.BadArgs:
                    case InteractionCommandError.Exception:
                    case InteractionCommandError.Unsuccessful:
                    case InteractionCommandError.ParseFailed:
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    private static Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess) return Task.CompletedTask;
        switch (result.Error)
        {
            case null:
            case InteractionCommandError.UnknownCommand:
            case InteractionCommandError.ConvertFailed:
            case InteractionCommandError.BadArgs:
            case InteractionCommandError.Exception:
            case InteractionCommandError.Unsuccessful:
            case InteractionCommandError.ParseFailed:
            case InteractionCommandError.UnmetPrecondition:
                // implement
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.CompletedTask;
    }
}
