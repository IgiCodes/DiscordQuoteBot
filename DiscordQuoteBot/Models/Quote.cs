namespace DiscordQuoteBot.Models;

internal class Quote
{
    public DateTime TimeAdded { get; set; } = DateTime.UtcNow;
    public ulong AddedBy { get; set; }
    public string QuoteText { get; set; } = "";
}
