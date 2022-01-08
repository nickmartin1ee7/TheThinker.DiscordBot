using Remora.Rest.Core;
using System.Collections.Generic;

namespace TheThinker.DiscordBot.WorkerService.Models;

public static class ShortTermMemory
{
    public static HashSet<Snowflake> KnownGuilds { get; } = new();
}