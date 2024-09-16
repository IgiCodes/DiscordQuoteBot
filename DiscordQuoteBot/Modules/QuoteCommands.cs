using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Text;
using DiscordQuoteBot.Models;
using DiscordQuoteBot.Services;
using JetBrains.Annotations;

namespace DiscordQuoteBot.Modules
{
    [PublicAPI]
    [Group("quote", "Commands relating to quotes")]
    public class QuoteCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public ServerDataService? ServerDataService { get; set; }
        public DiscordSocketClient? Client { get; set; }

        [SlashCommand("add", "Adds a quote")]
        public async Task AddQuote(string quote)
        {
            var list = ServerDataService!.GetDataForServer(Context.Guild.Id).QuoteList;
            list.Add(
                new Quote()
                {
                    AddedBy = Context.User.Id,
                    QuoteText = quote
                });
            ServerDataService.SaveData();
            await RespondAsync($"Added quote #{list.Count} `{quote}`");
        }

        [SlashCommand("info", "Provides quote information")]
        public async Task Info(int quoteNum = 0)
        {
            var list = ServerDataService!.GetDataForServer(Context.Guild.Id).QuoteList;
            if (quoteNum == 0)
            {
                //General information
                await RespondAsync($"{list.Count} quotes in database", ephemeral: true);
            }
            else
            {
                quoteNum--;
                if(quoteNum < 0 || quoteNum > list.Count)
                {
                    await RespondAsync($"Quote Number should be between 1 and {list.Count}", ephemeral: true);
                    return;
                }
                var adder = await Client!.GetUserAsync(list[quoteNum].AddedBy);
                await RespondAsync(
                    $"**Quote Number {quoteNum+1}**\n" +
                    $"Added by {adder.Mention}\n" + 
                    $"On {list[quoteNum].TimeAdded}",
                    ephemeral: true);
            }
        }
        [SlashCommand("list", "Lists up to 10 quotes per page")]
        public async Task List(int pageNum = 1)
        {
            pageNum--;
            var sb = CreateQuoteList(Context.Guild.Id, pageNum);

            if (BuildCompsForListPage(Context.Guild.Id, pageNum, out var builder))
            {
                await RespondAsync(sb.ToString(), ephemeral: true, components: builder.Build());
            }
            else
            {
                await RespondAsync(sb.ToString(), ephemeral: true);
            }
            Client!.ButtonExecuted += Client_ButtonExecuted;
        }

        private bool BuildCompsForListPage(ulong? guildId, int pageNum, out ComponentBuilder builder)
        {
            builder = new ComponentBuilder();
            var doButtons = false;
            
            var list = ServerDataService!.GetDataForServer(Context.Guild.Id).QuoteList;
            var maxPage = list.Count / 10;

            if (pageNum != 0)
            {
                builder = builder.WithButton("Previous", $"quote-list-button-{pageNum - 1}");
                doButtons = true;
            }
            if (pageNum < maxPage)
            {
                builder = builder.WithButton("Next", $"quote-list-button-{pageNum + 1}");
                doButtons = true;
            }

            return doButtons;
        }
        private StringBuilder CreateQuoteList(ulong? guildId, int pageNum)
        {
            var list = ServerDataService!.GetDataForServer(Context.Guild.Id).QuoteList;
            var maxPage = list.Count / 10;

            var sb = new StringBuilder();

            sb.AppendLine($"Quote page {pageNum + 1} of {maxPage + 1}");
            var remaining = list.Count - (pageNum * 10);

            for (var i = 0; i < Math.Min(10, remaining); i++)
            {
                var quoteNum = pageNum * 10 + i;
                sb.AppendLine($"{quoteNum + 1}: `{list[quoteNum].QuoteText}`");
            }

            return sb;
        }

        private Task Client_ButtonExecuted(SocketMessageComponent component)
        {
            if (!component.Data.CustomId.StartsWith("quote-list-button-")) return Task.CompletedTask;
            var pageToLoad = int.Parse(component.Data.CustomId.Substring(component.Data.CustomId.LastIndexOf('-') + 1));
            if (BuildCompsForListPage(component.GuildId, pageToLoad, out var builder))
            {
                component.UpdateAsync(comp =>
                {
                    comp.Content = CreateQuoteList(component.GuildId, pageToLoad).ToString();
                    comp.Components = builder.Build();
                });
            }
            else {
                component.UpdateAsync(comp =>
                {
                    comp.Content = CreateQuoteList(component.GuildId, pageToLoad).ToString();
                });
            }
            return Task.CompletedTask;
        }

        [SlashCommand("say", "Says a quote with optional ID")]
        public async Task SayQuote(int quoteNum = 0)
        {
            var list = ServerDataService!.GetDataForServer(Context.Guild.Id).QuoteList;
            if(quoteNum > list.Count)
            {
                await RespondAsync($"Number too high - max quote number is {list.Count}", ephemeral: true);
                return;
            }
            if(quoteNum == 0)
            {
                quoteNum = new Random().Next(list.Count);
            }
            else
            {
                quoteNum--;
            }

            await RespondAsync($"{quoteNum + 1}: `{list[quoteNum].QuoteText}`");
        }
    }
}
