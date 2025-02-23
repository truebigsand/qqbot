using ChangXingGeRevived.Extensions;
using Lagrange.Core;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChangXingGeRevived.CommandHandlers;

public class DeepSeekHandler(ILogger<DeepSeekHandler> logger) : ICommandHandler
{
    public Task HandleFriendAsync(BotContext bot, FriendMessageEvent e, string[] args)
    {
        throw new NotImplementedException();
    }

    public async Task HandleGroupAsync(BotContext bot, GroupMessageEvent e, string[] args)
    {
        if (args.Length == 0)
        {
            await e.ReplyAsync(bot, "请输入提示词");
            return;
        }
        var (reasoning_content, content) = await GetResult(string.Join(' ', args));
        if (reasoning_content == content || reasoning_content == string.Empty)
        {
            await e.ReplyAsync(bot, content);
        }
        else if (reasoning_content.Length + content.Length > 4000)
        {
            await e.ReplyAsync(bot, new FileEntity(Encoding.UTF8.GetBytes($"{reasoning_content}\n===============================\n{content}"), "result.txt"));
        }
        else
        {
            await e.ReplyAsync(bot, $"{reasoning_content}\n===============================\n{content}");
        }
        
    }

    private async Task<(string, string)> GetResult(string prompt)
    {
        string url = $"http://125.122.20.194:86/requestDeepSeek?content={prompt}"; // SSE endpoint URL

        using HttpClient client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "text/event-stream");

        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(responseStream);
        StringBuilder reasoning_content_builder = new(), content_builder = new();
        while (!reader.EndOfStream)
        {
            string line = await reader.ReadLineAsync();

            if (!string.IsNullOrWhiteSpace(line))
            {
                line = line.TrimStart(['d', 'a', 't', 'a', ':', ' ']).TrimEnd();
                if (line == "[DONE]")
                {
                    break;
                }
                var root = JsonSerializer.Deserialize<JsonElement>(line);
                if (root.TryGetProperty("error_msg", out var error_msg))
                {
                    return (string.Empty, error_msg.GetString());
                }
                var choices = root.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("reasoning_content", out var reasoning_content))
                    {
                        reasoning_content_builder.Append(reasoning_content.GetString());
                        //Console.Write(reasoning_content.GetString());
                    }
                    else if (delta.TryGetProperty("content", out var content))
                    {
                        content_builder.Append(content.GetString());
                        //Console.Write(content.GetString());
                    }
                    else
                    {
                        logger.LogError("Unknown property content: {}", delta.ToString());
                        //Console.Write(delta.ToString());
                    }
                }
            }
        }
        return (reasoning_content_builder.ToString().Trim(), content_builder.ToString().Trim());
    }
}
