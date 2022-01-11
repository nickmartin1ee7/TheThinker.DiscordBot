using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using DeepAI;

namespace TheThinker.DiscordBot.WorkerService.Commands;

public class ThinkerCommands : LoggedCommandGroup<ThinkerCommands>
{
    private readonly FeedbackService _feedbackService;
    private readonly DeepAI_API _aiClient;

    public ThinkerCommands(ILogger<ThinkerCommands> logger,
        FeedbackService feedbackService,
        ICommandContext ctx,
        IDiscordRestGuildAPI guildApi,
        IDiscordRestChannelAPI channelApi,
        DeepAI_API aiClient)
        : base(ctx, logger, guildApi, channelApi)
    {
        _feedbackService = feedbackService;
        _aiClient = aiClient;
    }

    [Command("think")]
    [CommandType(ApplicationCommandType.ChatInput)]
    [Description("Generate an AI story")]
    public async Task<IResult> ThinkAsync([Description("Enter a prompt to generate a story from")] string text)
    {
        await LogCommandUsageAsync(typeof(ThinkerCommands).GetMethod(nameof(ThinkAsync)), text);

        if (string.IsNullOrWhiteSpace(text))
        {
            var invalidReply =
                await _feedbackService.SendContextualErrorAsync("Your prompt cannot be empty.", ct: CancellationToken);
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        string? replyContent = null;
        var sw = new Stopwatch();

        sw.Start();
        try
        {
            replyContent = await Task.Run(() =>
                _aiClient.callStandardApi("text-generator", new {text})
                    .output?.ToString(), CancellationToken);
        }
        catch (TaskCanceledException) {} // Allowed to be thrown if response is no longer needed
        finally
        {
            sw.Stop();
        }

        _logger.LogDebug("DeepAI took {responseElapsed} for the response: {response}", sw.Elapsed, replyContent);

        if (string.IsNullOrWhiteSpace(replyContent))
        {
            var noStoryResponse = await _feedbackService.SendContextualNeutralAsync("I had no thoughts on that prompt.", ct: CancellationToken);
            return noStoryResponse.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(noStoryResponse);
        }

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("I was thinking...",
            Description: replyContent),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }
}