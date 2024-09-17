using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Text;
using DiscordQuoteBot.Data;
using DiscordQuoteBot.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DiscordQuoteBot.Modules
{
    [PublicAPI]
    [Group("quote", "Commands relating to quotes")]
    public class QuoteCommands(QuoteBotDbContext db, DiscordSocketClient client)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("add", "Adds a quote")]
        public async Task AddQuote([MinLength(1)] string quote)
        {
            var newQuote = db.Quotes.Add(new Quote()
            {
                AuthorId = Context.User.Id,
                GuildId = Context.Guild.Id,
                QuoteText = quote
            });
            await db.SaveChangesAsync();

            await RespondAsync($"Added quote #{newQuote.Entity.Id} `{quote}`");
        }

        [SlashCommand("info", "Provides quote information")]
        public async Task Info([MinValue(1)] int quoteId = 0)
        {
            if (quoteId is 0)
            {
                var quoteCount = await db.Quotes.CountAsync();
                //General information
                await RespondAsync($"{quoteCount} quotes in database", ephemeral: true);
                return;
            }

            var quote = await db.Quotes.FindAsync(quoteId);

            if (quote is null)
            {
                await RespondAsync($"Quote with ID {quoteId} could not be found.", ephemeral: true);
                return;
            }

            try
            {
                var quoteAuthor = await Context.Client.GetUserAsync(quote.AuthorId);
                var responseMessage = $"**Quote Number {quote.Id}**\n";
                responseMessage += quoteAuthor is null
                    ? $"Added on {quote.TimeAdded}"
                    : $"Added by {quoteAuthor.Mention}\n" +
                      $"On {quote.TimeAdded}";
                await RespondAsync(responseMessage, ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [SlashCommand("list", "Lists up to 10 quotes per page")]
        public async Task List([MinValue(1)] int pageNumber = 1)
        {
            var quoteCount = await db.Quotes.CountAsync();
            var maxPage = quoteCount / 10 + 1;
            pageNumber = Math.Min(pageNumber, maxPage);
            var pageIndex = pageNumber - 1;
            var pageQuoteIndex = pageIndex * 10;

            var sb = await CreateQuoteList(Context.Guild.Id, pageNumber, pageQuoteIndex, maxPage);
            var builder = new ComponentBuilder();
            BuildCompsForListPage(Context.Guild.Id, pageIndex, maxPage, builder);
            await RespondAsync(sb.ToString(), ephemeral: true, components: builder.Build());

            client.ButtonExecuted += Client_ButtonExecuted;
        }

        private static void BuildCompsForListPage(ulong? guildId, int pageIndex, int maxPage, ComponentBuilder builder)
        {
            if (pageIndex > 0)
            {
                builder = builder.WithButton("Previous", $"quote-list-button-{pageIndex - 1}");
            }

            if (pageIndex + 1 >= maxPage) return;

            builder.WithButton("Next", $"quote-list-button-{pageIndex + 1}");
        }

        private async Task<StringBuilder> CreateQuoteList(ulong? guildId, int pageNumber, int pageQuoteIndex, int maxPage)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Quote page {pageNumber} of {maxPage}");

            var quotes = await db.Quotes.OrderBy(quote => quote.Id).Skip(pageQuoteIndex).Take(10).ToListAsync();
            foreach (var quote in quotes)
            {
                sb.AppendLine($"{quote.Id}: `{quote.QuoteText}`");
            }

            return sb;
        }

        private async Task Client_ButtonExecuted(SocketMessageComponent component)
        {
            if (!component.Data.CustomId.StartsWith("quote-list-button-")) return;

            var quoteCount = await db.Quotes.CountAsync();
            var maxPage = quoteCount / 10 + 1;
            var pageToLoad = int.Parse(component.Data.CustomId[(component.Data.CustomId.LastIndexOf('-') + 1)..]);
            var pageIndex = pageToLoad - 1;
            var pageQuoteIndex = pageIndex * 10;

            var builder = new ComponentBuilder();
            BuildCompsForListPage(component.GuildId, pageToLoad, maxPage, builder);
            await component.UpdateAsync(comp =>
            {
                comp.Content = CreateQuoteList(component.GuildId, pageToLoad, pageQuoteIndex, maxPage).ToString();
                comp.Components = builder.Build();
            });
        }

        [SlashCommand("say", "Says a quote with optional ID")]
        public async Task SayQuote([MinValue(1)] int quoteId = 0)
        {
            if (quoteId is 0)
            {
                quoteId = new Random().Next(1, await db.Quotes.CountAsync() + 1);
            }

            var quote = await db.Quotes.FindAsync(quoteId);
            if (quote is null)
            {
                await RespondAsync($"Could not find quote with ID: {quoteId}", ephemeral: true);
                return;
            }

            await RespondAsync($"#{quote.Id}: `{quote.QuoteText}`");
        }
    }
}
