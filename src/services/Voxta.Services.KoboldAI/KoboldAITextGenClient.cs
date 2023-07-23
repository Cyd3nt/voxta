﻿using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Voxta.Services.OpenAI;

namespace Voxta.Services.KoboldAI;

public class KoboldAITextGenClient : ITextGenService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private readonly Sanitizer _sanitizer;
    private readonly IPerformanceMetrics _performanceMetrics;

    public KoboldAITextGenClient(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, Sanitizer sanitizer, IPerformanceMetrics performanceMetrics)
    {
        _httpClient = httpClientFactory.CreateClient($"{KoboldAIConstants.ServiceName}.TextGen");
        _settingsRepository = settingsRepository;
        _sanitizer = sanitizer;
        _performanceMetrics = performanceMetrics;
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync<KoboldAISettings>(cancellationToken);
        var uri = settings?.Uri;
        if (string.IsNullOrEmpty(uri)) throw new KoboldAIException("Missing uri in settings");
        _httpClient.BaseAddress = new Uri(uri);
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    public async ValueTask<TextData> GenerateReplyAsync(IReadOnlyChatSessionData chatSessionData, CancellationToken cancellationToken)
    {
        var builder = new GenericPromptBuilder();
        // TODO: count tokens?
        var prompt = builder.BuildReplyPrompt(chatSessionData, -1);
        var body = new
        {
            use_story = false,
            use_memory = false,
            use_authors_note = false,
            use_world_info = false,
            max_length = 80,
            rep_pen = 1.08,
            rep_pen_range = 1024,
            rep_pen_slope = 0.9,
            tfs = 0.9,
            temperature = 0.65,
            top_p = 0.9,
            sampler_order = new[] { 6, 0, 1, 2, 3, 4, 5 },
            prompt,
            stop_sequence = new[] { "END_OF_DIALOG", "You:", $"{chatSessionData.UserName}:", $"{chatSessionData.Character.Name}:", "\n" }
        };
        var bodyContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/extra/generate/stream");
        request.Content = bodyContent;

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var textGenPerf = _performanceMetrics.Start("KoboldAI.TextGen");
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new KoboldAIException(await response.Content.ReadAsStringAsync(cancellationToken));

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        #warning Extract to common utility for SSE
        var sb = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (!line.StartsWith("data:")) continue;
            var json = JsonSerializer.Deserialize<TextGenEventData>(line[5..]);
            if (json == null) break;
            // TODO: Determine which tokens are considered end tokens.
            var token = json.token;
            sb.Append(token);
            // TODO: Determine a rule of thumb for when to stop.
            // if (sb.Length > 40 && json.token.Contains('.') || json.token.Contains('!') || json.token.Contains('?')) break;
        }
        reader.Close();
        
        textGenPerf.Done();

        var text = sb.ToString();
        var sanitized = _sanitizer.Sanitize(text);
        
        return new TextData
        {
            Text = sanitized,
            Tokens = 0,
        };
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private class TextGenEventData
    {
        public required string token { get; init; }
        public bool final { get; init; }
        public int ptr { get; init; }
        public string? error { get; init; }
    }

    public void Dispose()
    {
    }
}