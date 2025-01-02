using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.LMStudio;

public class LMStudioActionInferenceService : LMStudioClientBase, IActionInferenceService
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IServiceObserver _serviceObserver;

    public LMStudioActionInferenceService(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, IPerformanceMetrics performanceMetrics, IServiceObserver serviceObserver)
        :base(httpClientFactory, settingsRepository)
    {
        _performanceMetrics = performanceMetrics;
        _serviceObserver = serviceObserver;
    }

    public async ValueTask<string> SelectActionAsync(IChatInferenceData chat, CancellationToken cancellationToken)
    {
        var builder = TextPromptBuilderFactory.Create(Settings.PromptFormat, Tokenizer);
        var prompt = builder.BuildActionInferencePromptString(chat);
        _serviceObserver.Record(ServiceObserverKeys.ActionInferenceService, LMStudioConstants.ServiceName);
        _serviceObserver.Record(ServiceObserverKeys.ActionInferencePrompt, prompt);
        
        var actionInferencePerf = _performanceMetrics.Start($"{LMStudioConstants.ServiceName}.ActionInference");
        var stoppingStrings = new[] { "]" };
        var action = await SendCompletionRequest(BuildRequestBody(prompt, stoppingStrings), cancellationToken);
        actionInferencePerf.Done();
        
        var result = action.TrimContainerAndToLower();
        _serviceObserver.Record(ServiceObserverKeys.ActionInferenceResult, result);
        return result;
    }
}