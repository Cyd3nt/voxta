﻿using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Services;
using Voxta.Services.ElevenLabs;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddElevenLabs(this IServiceCollection services)
    {
        services.AddTransient<ElevenLabsTextToSpeechClient>();
    }
    
    public static void RegisterElevenLabs(this IServiceRegistry<ITextToSpeechService> registry)
    {
        registry.Add<ElevenLabsTextToSpeechClient>(ElevenLabsConstants.ServiceName);
    }
}