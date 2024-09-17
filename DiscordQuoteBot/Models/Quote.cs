using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DiscordQuoteBot.Models;

[Index(nameof(Id), nameof(GuildId))]
public class Quote
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public DateTime TimeAdded { get; set; } = DateTime.UtcNow;

    public ulong AuthorId { get; set; }

    [MaxLength(500)]
    public string QuoteText { get; set; } = "";
}
