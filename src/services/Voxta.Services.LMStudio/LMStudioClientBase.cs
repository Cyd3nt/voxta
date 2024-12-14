using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Voxta.Abstractions.Repositories;
using Voxta.Shared.RemoteServicesUtils;

namespace Voxta.Services.LMStudio;

public class LMStudioClientBase : RemoteLLMServiceClientBase<LMStudioSettings, LMStudioParameters, LMStudioRequestBody>
{
    protected override string ServiceName => LMStudioConstants.ServiceName;
    
    protected override string GenerateRequestPath => "/v1/completions";
    
    protected LMStudioClientBase(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository)
        : base(httpClientFactory, settingsRepository)
    {
    }

    protected LMStudioRequestBody BuildRequestBody(string prompt, string[] stoppingStrings)
    {
        var body = CreateParameters();
        body.Prompt = prompt;
        body.StoppingStrings = stoppingStrings;
        return body;
    }

    protected async Task<string> SendCompletionRequest(LMStudioRequestBody body, CancellationToken cancellationToken)
    {
        var json = await SendCompletionRequest<CompletionResponse>(body, cancellationToken);
        var text = json.Choices?[0].Text ?? throw new LMStudioException("Empty response");
        return text.TrimExcess();
    }

    public class Logprobs
    {
        [JsonPropertyName("tokens")]
        public List<string>? Tokens { get; set; }
        [JsonPropertyName("token_logprobs")]
        public List<double>? TokenLogprobs { get; set; }
        [JsonPropertyName("top_logprobs")]
        public List<Dictionary<string, double>>? TopLogprobs { get; set; }
        [JsonPropertyName("text_offset")]
        public List<int>? TextOffset { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        [JsonPropertyName("index")] 
        public int Index { get; set; }
        [JsonPropertyName("logprobs")] 
        public Logprobs? Logprobs { get; set; }
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class CompletionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("object")]
        public string? Object { get; set; }
        [JsonPropertyName("created")]
        public int Created { get; set; }
        [JsonPropertyName("model")] 
        public string? Model { get; set; }
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }
}