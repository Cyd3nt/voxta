﻿using ChatMate.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatMate.Server;

public class Program
{
    public static async Task Main(string[] args) => await Start(args, CancellationToken.None);

    public static async Task Start(string[] args, CancellationToken cancellationToken)
    {
        var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();

        var server = serviceProvider.GetRequiredService<ChatMateServer>();

        Console.CancelKeyPress += (sender, cancelArgs) =>
        {
            server.Stop();
            cancelArgs.Cancel = true;
        };

        await server.Start(cancellationToken);
    }

    private static void ConfigureServices(IServiceCollection services, IConfigurationRoot configuration)
    {
        services.AddHttpClient();
        services.Configure<ChatMateServerOptions>(configuration.GetSection("ChatMate.Server"));
        services.AddSingleton<ChatMateServer>();
        services.AddSingleton<ChatMateConnectionFactory>();
        services.AddTransient<ChatMateConnection>();
        
        services.Configure<NovelAIOptions>(configuration.GetSection("ChatMate.Services:NovelAI"));
        services.AddSingleton<NovelAIClient>();

        services.AddSingleton<ITextGenService>(sp => sp.GetRequiredService<NovelAIClient>());
        services.AddSingleton<ITextToSpeechService>(sp => sp.GetRequiredService<NovelAIClient>());
    }
}