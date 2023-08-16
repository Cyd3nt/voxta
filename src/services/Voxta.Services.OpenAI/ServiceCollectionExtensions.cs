﻿using Voxta.Abstractions.Services;
using Voxta.Services.OpenAI;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddOpenAI(this IServiceCollection services)
    {
        services.AddTransient<OpenAITextGenClient>();
        services.AddTransient<OpenAIActionInferenceClient>();
        services.AddTransient<OpenAISummarizationService>();
    }
    
    public static void RegisterOpenAI(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = OpenAIConstants.ServiceName,
            Label = "OpenAI",
            TextGen = true,
            STT = false,
            TTS = false,
            Summarization = true,
            ActionInference = true,
            SettingsType = typeof(OpenAISettings),
        });
    }
    
    public static void RegisterOpenAI(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<OpenAITextGenClient>(OpenAIConstants.ServiceName);
    }
    
    public static void RegisterOpenAI(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<OpenAIActionInferenceClient>(OpenAIConstants.ServiceName);
    }
    
    public static void RegisterOpenAI(this IServiceRegistry<ISummarizationService> registry)
    {
        registry.Add<OpenAISummarizationService>(OpenAIConstants.ServiceName);
    }
}