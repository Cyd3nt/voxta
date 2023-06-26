﻿using ChatMate.Abstractions.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Data.Yaml;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYamlRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IBotRepository, BotYamlFileRepository>();
        services.AddSingleton<ISettingsRepository, SettingsYamlFileRepository>();
        services.AddSingleton<IProfileRepository, ProfileYamlFileRepository>();
        return services;
    }
}