﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Common;

namespace Voxta.Services.KoboldAI;

public class KoboldAIClientBase
{
    private static readonly IMapper Mapper;
    
    static KoboldAIClientBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<KoboldAIParameters, KoboldAIRequestBody>();
        });
        Mapper = config.CreateMapper();
    }
    
    public string ServiceName => KoboldAIConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };
    
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private KoboldAIParameters? _parameters;
    private bool _initialized;

    protected KoboldAIClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
    {
        _httpClient = httpClientFactory.CreateClient(KoboldAIConstants.ServiceName);
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> InitializeAsync(string[] prerequisites, string culture, CancellationToken cancellationToken)
    {
        if (_initialized) return true;
        _initialized = true;
        var settings = await _settingsRepository.GetAsync<KoboldAISettings>(cancellationToken);
        if (settings == null) return false;
        if (!settings.Enabled) return false;
        var uri = settings.Uri;
        if (string.IsNullOrEmpty(uri)) return false;
        _httpClient.BaseAddress = new Uri(uri);
        _parameters = settings.Parameters ?? new KoboldAIParameters();
        return true;
    }

    public int GetTokenCount(string message)
    {
        return 0;
    }

    protected async Task<string> SendCompletionRequest(string prompt, string[] stoppingStrings, CancellationToken cancellationToken)
    {
        var body = Mapper.Map<KoboldAIRequestBody>(_parameters);
        body.Prompt = prompt;
        body.StopSequence = stoppingStrings;

        var bodyContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/extra/generate/stream");
        request.Content = bodyContent;
        request.ConfigureEvenStream();
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new KoboldAIException(await response.Content.ReadAsStringAsync(cancellationToken));

        return await response.ReadEventStream<TextGenEventData>(cancellationToken);
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class TextGenEventData : IEventStreamData
    {
        public required string token { get; init; }
        public bool final { get; init; }
        public int ptr { get; init; }
        public string? error { get; init; }
        public string GetToken() => token;
    }

    public void Dispose()
    {
    }
}