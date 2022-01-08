﻿using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace TheThinker.DiscordBot.WorkerService.Commands;

public class ThinkerCommands : LoggedCommandGroup<ThinkerCommands>, IDisposable
{
    private readonly FeedbackService _feedbackService;
    private readonly HttpClient _httpClient;

    public ThinkerCommands(ILogger<ThinkerCommands> logger,
        FeedbackService feedbackService,
        ICommandContext ctx,
        IDiscordRestGuildAPI guildApi,
        IDiscordRestChannelAPI channelApi,
        HttpClient httpClient)
        : base(ctx, logger, guildApi, channelApi)
    {
        _feedbackService = feedbackService;
        _httpClient = httpClient;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
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

        var sw = new Stopwatch();
        sw.Start();
        var httpMsg = new HttpRequestMessage(HttpMethod.Post, $"https://api.deepai.org/api/text-generator"); //todo
        httpMsg.Content = new StringContent($"{{ \"text\": \"{text}\" }}", System.Text.Encoding.UTF8, "application/json");
        var httpResponse = await _httpClient.SendAsync(httpMsg);
        var replyContent = await httpResponse.Content.ReadAsStringAsync();
        sw.Stop();

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