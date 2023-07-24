﻿using System.Runtime.ExceptionServices;
using Voxta.Abstractions.DependencyInjection;
using Voxta.Abstractions.Diagnostics;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Network;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;
using Voxta.Common;
using Microsoft.Extensions.Logging;

namespace Voxta.Core;

public class ChatSessionFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly IProfileRepository _profileRepository;
    private readonly SpeechGeneratorFactory _speechGeneratorFactory;
    private readonly IServiceFactory<ITextGenService> _textGenFactory;
    private readonly IServiceFactory<ITextToSpeechService> _textToSpeechFactory;
    private readonly IServiceFactory<IActionInferenceService> _animationSelectionFactory;
    private readonly IServiceFactory<ISpeechToTextService> _speechToTextServiceFactory;

    public ChatSessionFactory(
        ILoggerFactory loggerFactory,
        IPerformanceMetrics performanceMetrics,
        IProfileRepository profileRepository,
        SpeechGeneratorFactory speechGeneratorFactory,
        IServiceFactory<ITextGenService> textGenFactory,
        IServiceFactory<ITextToSpeechService> textToSpeechFactory,
        IServiceFactory<IActionInferenceService> animationSelectionFactory,
        IServiceFactory<ISpeechToTextService> speechToTextServiceFactory
        )
    {
        _loggerFactory = loggerFactory;
        _performanceMetrics = performanceMetrics;
        _profileRepository = profileRepository;
        _speechGeneratorFactory = speechGeneratorFactory;
        _textGenFactory = textGenFactory;
        _textToSpeechFactory = textToSpeechFactory;
        _animationSelectionFactory = animationSelectionFactory;
        _speechToTextServiceFactory = speechToTextServiceFactory;
        _profileRepository = profileRepository;
    }

    public async Task<IChatSession> CreateAsync(IUserConnectionTunnel tunnel, ClientStartChatMessage startChatMessage, CancellationToken cancellationToken)
    {
        ITextGenService? textGen = null;
        ISpeechToTextService? speechToText = null;
        IActionInferenceService? actionInference = null;
        ISpeechGenerator? speechGenerator = null;

        try
        {
            if (startChatMessage.AudioPath != null)
            {
                Directory.CreateDirectory(startChatMessage.AudioPath);
            }
            
            var profile = await _profileRepository.GetProfileAsync(cancellationToken);
            if (profile == null) throw new InvalidOperationException("Cannot start chat, no profile is set.");
            var useSpeechRecognition = startChatMessage.UseServerSpeechRecognition && !string.IsNullOrEmpty(profile.Services.SpeechToText.Service);

            textGen = await _textGenFactory.CreateAsync(startChatMessage.TextGenService, startChatMessage.Culture, cancellationToken);
            speechToText = useSpeechRecognition ? await _speechToTextServiceFactory.CreateAsync(profile.Services.SpeechToText.Service, startChatMessage.Culture, cancellationToken) : null;
            actionInference = string.IsNullOrEmpty(profile.Services.ActionInference.Service)
                ? null
                : await _animationSelectionFactory.CreateAsync(profile.Services.ActionInference.Service, startChatMessage.Culture, cancellationToken);

            var textProcessor = new ChatTextProcessor(profile, startChatMessage.Name);
            
            string[]? thinkingSpeech = null;
            if (startChatMessage is { TtsService: not null, TtsVoice: not null })
            {
                var textToSpeechGen = await _textToSpeechFactory.CreateAsync(startChatMessage.TtsService, startChatMessage.Culture, cancellationToken);
                thinkingSpeech = textToSpeechGen.GetThinkingSpeech();
            }

            speechGenerator = await _speechGeneratorFactory.CreateAsync(startChatMessage.TtsService, startChatMessage.TtsVoice, startChatMessage.Culture, startChatMessage.AudioPath, startChatMessage.AcceptedAudioContentTypes, cancellationToken);

            // TODO: Use a real chat data store, reload using auth
            var chatData = new ChatSessionData
            {
                ChatId = startChatMessage.ChatId ?? Crypto.CreateCryptographicallySecureGuid(),
                UserName = profile.Name,
                Character = new CharacterCard
                {
                    Name = startChatMessage.Name,
                    Description = textProcessor.ProcessText(startChatMessage.Description),
                    Personality = textProcessor.ProcessText(startChatMessage.Personality),
                    Scenario = textProcessor.ProcessText(startChatMessage.Scenario),
                    FirstMessage = textProcessor.ProcessText(startChatMessage.FirstMessage),
                    MessageExamples = textProcessor.ProcessText(startChatMessage.MessageExamples),
                    SystemPrompt = textProcessor.ProcessText(startChatMessage.SystemPrompt),
                    PostHistoryInstructions = textProcessor.ProcessText(startChatMessage.PostHistoryInstructions),
                },
                ThinkingSpeech = thinkingSpeech,
                AudioPath = startChatMessage.AudioPath,
                TtsVoice = startChatMessage.TtsVoice
            };
            // TODO: Optimize by pre-calculating tokens count

            return new ChatSession(
                tunnel,
                _loggerFactory,
                _performanceMetrics,
                textGen,
                chatData,
                textProcessor,
                profile,
                new ChatSessionState(),
                speechGenerator,
                actionInference,
                speechToText
            );
        }
        catch (Exception exc)
        {
            textGen?.Dispose();
            speechToText?.Dispose();
            actionInference?.Dispose();
            speechGenerator?.Dispose();
            ExceptionDispatchInfo.Capture(exc).Throw();
            throw;
        }
    }
}