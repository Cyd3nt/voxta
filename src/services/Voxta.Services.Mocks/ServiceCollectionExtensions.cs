﻿using Voxta.Abstractions.Services;
using Voxta.Services.Mocks;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddMocks(this IServiceCollection services)
    {
        services.AddTransient<MockTextGenService>();
        services.AddTransient<MockTextToSpeechService>();
        services.AddTransient<MockActionInferenceService>();
        services.AddTransient<MockSummarizationService>();
    }
    
    public static void RegisterMocks(this IServiceDefinitionsRegistry registry)
    {
        registry.Add(new ServiceDefinition
        {
            ServiceName = MockConstants.ServiceName,
            Label = "Mocks (Debug Only)",
            TextGen = true,
            STT = false,
            TTS = true,
            Summarization = true,
            ActionInference = true,
            SettingsType = typeof(MockSettings),
        });
    }
    
    public static void RegisterMocks(this IServiceRegistry<ITextGenService> registry)
    {
        registry.Add<MockTextGenService>(MockConstants.ServiceName);
    }
    
    public static void RegisterMocks(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<MockTextToSpeechService>(MockConstants.ServiceName);
    }
    
    public static void RegisterMocks(this IServiceRegistry<IActionInferenceService> registry)
    {
        registry.Add<MockActionInferenceService>(MockConstants.ServiceName);
    }
    
    public static void RegisterMocks(this IServiceRegistry<ISummarizationService> registry)
    {
        registry.Add<MockSummarizationService>(MockConstants.ServiceName);
    }
}