using Moq;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.System;
using Voxta.Shared.LLMUtils;

namespace Voxta.Services.NovelAI.Tests;

public class NovelAIPromptBuilderTests
{
    private NovelAIPromptBuilder _builder = null!;

    [SetUp]
    public void Setup()
    {
        var tokenizer = new AverageTokenizer();
        var timeProvider = new Mock<ITimeProvider>();
        timeProvider.Setup(m => m.LocalNow).Returns(new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero));
        _builder = new NovelAIPromptBuilder(tokenizer, timeProvider.Object);
    }

    [Test]
    public void TestPromptMinimal()
    {
        var actual = _builder.BuildReplyPrompt(
            new ChatSessionData
                {
                    Culture = "en-US",
                    User = new ChatSessionDataUser { Name = "Joe" },
                    Chat = new Chat
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = Guid.NewGuid(),
                    },
                    Character = new()
                    {
                        Name = "Jane",
                        Description = "some-description",
                        Personality = "some-personality",
                        Scenario = "some-scenario",
                        FirstMessage = "some-first-message",
                    }
                }
                .AddMessage("Joe", "Hello")
                .AddMessage("Jane", "World")
                .AddMessage("Joe", "Question"),
            0, 2000);

        Assert.That(actual, Is.EqualTo("""
        This is a spoken conversation between Joe and Jane. You are playing the role of Jane. The current date and time is Saturday, February 3, 2001 4:05 AM. Keep the conversation flowing, actively engage with Joe. Stay in character. Emojis are prohibited, only use spoken words. Avoid making up facts about Joe.
        
        Description of Jane: some-description
        Personality of Jane: some-personality
        Circumstances and context of the dialogue: some-scenario
        ***
        [ Style: chat ]
        Joe: Hello
        Jane: World
        Joe: Question
        Jane:
        """.ReplaceLineEndings("\n")));
    }
}