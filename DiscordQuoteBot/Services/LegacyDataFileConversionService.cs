using System.Text.Json;
using DiscordQuoteBot.Models;
using Microsoft.Extensions.Hosting;

namespace DiscordQuoteBot.Services;

public class LegacyDataFileConversionService(ServerDataService serverDataService) : BackgroundService
{
    private const string OldQuoteFile = "quotes.json";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!File.Exists(OldQuoteFile))
        {
            return;
        }

        if (JsonSerializer.Deserialize<Dictionary<ulong, List<Quote>>>(await File.ReadAllTextAsync(OldQuoteFile)) is
            { } loadedData)
        {
            foreach (var v in loadedData)
            {
                var d = serverDataService.GetDataForServer(v.Key);
                foreach (var q in v.Value)
                {
                    d.QuoteList.Add(q);
                }
            }
        }

        serverDataService.SaveData();
        File.Delete(OldQuoteFile);
    }
}
