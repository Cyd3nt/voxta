﻿using Voxta.Abstractions.Services;
using Voxta.Services.KoboldAI;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddKoboldAI(this IServiceCollection services)
    {
        services.AddTransient<KoboldAITextGenService>();
        services.AddTransient<KoboldAIActionInferenceService>();
        services.AddTransient<KoboldAISummarizationService>();
    }
    
    public static void RegisterKoboldAI(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = KoboldAIConstants.ServiceName,
            Label = "KoboldAI",
            TextGen = true,
            STT = false,
            TTS = false,
            Summarization = true,
            ActionInference = true,
            SettingsType = typeof(KoboldAISettings),
        });
    }
    
    public static void RegisterKoboldAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<KoboldAITextGenService>(KoboldAIConstants.ServiceName);
    }
    
    public static void RegisterKoboldAI(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<KoboldAIActionInferenceService>(KoboldAIConstants.ServiceName);
    }
    
    public static void RegisterKoboldAI(this IServiceRegistry<ISummarizationService> registry)
    {
        registry.Add<KoboldAISummarizationService>(KoboldAIConstants.ServiceName);
    }
}