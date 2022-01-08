﻿using System;
using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using DeepAI;
using TheThinker.DiscordBot.WorkerService.Extensions;

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
            var invalidReply = await _feedbackService.SendContextualErrorAsync("Your prompt cannot be empty.", ct: CancellationToken);
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        string replyContent = replyContent = await Task.Run(() =>
            _aiClient.callStandardApi("text-generator", new {text})
                .output?.ToString());

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