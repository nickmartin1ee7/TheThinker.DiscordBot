namespace TheThinker.DiscordBot.WorkerService.Models;

public class DiscordSettings
{
    public ulong? OwnerId { get; set; }
    public ulong? DebugServerId { get; set; }
    public string Status { get; set; }
    public string Token { get; set; }
    public string ShardManagerUri { get; set; }
}