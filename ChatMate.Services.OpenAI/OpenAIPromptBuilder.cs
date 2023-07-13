﻿using System.Text;
using ChatMate.Abstractions.Model;
using Microsoft.DeepDev;

namespace ChatMate.Services.OpenAI;

public class OpenAIPromptBuilder
{
    private readonly ITokenizer? _tokenizer;

    public OpenAIPromptBuilder(ITokenizer? tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public List<OpenAIMessage> BuildReplyPrompt(IReadOnlyChatSessionData chatSessionData, int maxTokens)
    {
        var systemPrompt = MakeSystemPrompt(chatSessionData.Character);
        var systemPromptTokens = _tokenizer?.Encode(systemPrompt, OpenAISpecialTokens.Keys).Count ?? 0;
        var postHistoryPrompt = MakePostHistoryPrompt(chatSessionData.Character, chatSessionData.Context, chatSessionData.Actions);
        var postHistoryPromptTokens = _tokenizer?.Encode(postHistoryPrompt, OpenAISpecialTokens.Keys).Count ?? 0;

        var totalTokens = systemPromptTokens + postHistoryPromptTokens;
        
        var messages = new List<OpenAIMessage> { new() { role = "system", content = systemPrompt } };
        var chatMessages = chatSessionData.GetMessages();
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var message = chatMessages[i];
            totalTokens += message.Tokens + 4; // https://github.com/openai/openai-python/blob/main/chatml.md
            if (totalTokens >= maxTokens) break;
            var role = message.User == chatSessionData.Character.Name ? "assistant" : "user";
            messages.Insert(1, new() { role = role, content = message.Text });
        }

        if (!string.IsNullOrEmpty(postHistoryPrompt))
            messages.Add(new() { role = "system", content = postHistoryPrompt });

        return messages;
    }

    public List<OpenAIMessage> BuildActionInferencePrompt(ChatSessionData chatSessionData)
    {
        var sb = new StringBuilder();
        sb.AppendLineLinux(chatSessionData.Character.Name + "'s Personality: " + chatSessionData.Character.Personality);
        sb.AppendLineLinux("Scenario: " + chatSessionData.Character.Scenario);
        sb.AppendLineLinux("Previous messages:");
        foreach (var message in chatSessionData.Messages.TakeLast(4))
        {
            sb.AppendLineLinux($"{message.User}: {message.Text}");
        }

        sb.AppendLineLinux("---");
        if (!string.IsNullOrEmpty(chatSessionData.Context))
            sb.AppendLineLinux($"Context: {chatSessionData.Context}");
        if (chatSessionData.Actions is { Length: > 1 })
            sb.AppendLineLinux($"Available actions: {string.Join(", ", chatSessionData.Actions.Select(a => $"[{a}]"))}");
        sb.AppendLineLinux($"Write the action {chatSessionData.Character.Name} should play.");
        var messages = new List<OpenAIMessage>
        {
            new() { role = "system", content = "You are a tool that selects the character animation for a Virtual Reality game. You will be presented with a chat, and must provide the animation to play from the provided list. Only answer with a single animation name. Example response: [smile]" },
            new() { role = "user", content = sb.ToString().TrimExcess() }
        };
        return messages;
    }

    private static string MakeSystemPrompt(CharacterCard character)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(character.SystemPrompt))
            sb.AppendLineLinux(character.SystemPrompt);
        if (!string.IsNullOrEmpty(character.Description))
            sb.AppendLineLinux($"Description of {character.Name}: {character.Description}");
        if (!string.IsNullOrEmpty(character.Personality))
            sb.AppendLineLinux($"Personality of {character.Name}: {character.Personality}");
        if (!string.IsNullOrEmpty(character.Scenario))
            sb.AppendLineLinux($"Circumstances and context of the dialogue: {character.Scenario}");
        return sb.ToString().TrimExcess();
    }

    private static string MakePostHistoryPrompt(CharacterCard character, string? context, string[]? actions)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(character.PostHistoryInstructions))
            sb.AppendLineLinux(character.PostHistoryInstructions);
        if (!string.IsNullOrEmpty(context))
            sb.AppendLineLinux($"Current context: {context}");
        if (actions is { Length: > 1 })
            sb.AppendLineLinux($"Available actions to be inferred after the response: {string.Join(", ", actions)}");
        return sb.ToString().TrimExcess();
    }
}