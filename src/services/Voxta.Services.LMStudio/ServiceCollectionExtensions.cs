using Voxta.Abstractions.Model;
using Voxta.Abstractions.Services;
using Voxta.Services.LMStudio;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddLMStudio(this IServiceCollection services)
    {
        services.AddTransient<LMStudioTextGenService>();
        services.AddTransient<LMStudioActionInferenceService>();
        services.AddTransient<LMStudioSummarizationService>();
    }
    
    public static void RegisterLMStudio(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = LMStudioConstants.ServiceName,
            Label = "LMStudio Text Generation Web UI",
            TextGen = ServiceDefinitionCategoryScore.Medium,
            STT = ServiceDefinitionCategoryScore.NotSupported,
            TTS = ServiceDefinitionCategoryScore.NotSupported,
            Summarization = ServiceDefinitionCategoryScore.Medium,
            ActionInference = ServiceDefinitionCategoryScore.Medium,
            Features = new[] { ServiceFeatures.NSFW },
            Recommended = true,
            Notes = "One of the most popular ways to run your own local large language models.",
            SettingsType = typeof(LMStudioSettings),
        });
    }
    
    public static void RegisterLMStudio(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<LMStudioTextGenService>(LMStudioConstants.ServiceName);
    }
    
    public static void RegisterLMStudio(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<LMStudioActionInferenceService>(LMStudioConstants.ServiceName);
    }
    
    public static void RegisterLMStudio(this IServiceRegistry<ISummarizationService> registry)
    {
        registry.Add<LMStudioSummarizationService>(LMStudioConstants.ServiceName);
    }
}
