using Voxta.Abstractions.Model;
using NUnit.Framework;
using Voxta.Services.OpenSourceLargeLanguageModels;

namespace Voxta.Services.OpenAI.Tests;

public class GenericPromptBuilderTests
{
    private GenericPromptBuilder _builder = null!;

    [SetUp]
    public void Setup()
    {
        _builder = new GenericPromptBuilder();
    }

    [Test]
    public void TestPromptMinimal()
    {
        var actual = _builder.BuildReplyPrompt(new ChatSessionData
        {
            UserName = "Joe",
            Character = new()
            {
                Name = "Jane",
                Description = "some-description",
                Personality = "some-personality",
                Scenario = "some-scenario",
                FirstMessage = "some-first-message",
            },
            Messages =
            {
                new ChatMessageData { User = "Joe", Text = "Hello" },
                new ChatMessageData { User = "Jane", Text = "World" },
                new ChatMessageData { User = "Joe", Text = "Question" },
            }
        }, 4096);

        Assert.That(actual, Is.EqualTo("""
        Description of Jane: some-description
        Personality of Jane: some-personality
        Circumstances and context of the dialogue: some-scenario
        Joe: Hello
        Jane: World
        Joe: Question
        Jane: 
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
    
    [Test]
    public void TestPromptFull()
    {
        var actual = _builder.BuildReplyPrompt(new ChatSessionData
        {
            UserName = "Joe",
            Character = new()
            {
                Name = "Jane",
                Description = "some-description",
                Personality = "some-personality",
                Scenario = "some-scenario",
                FirstMessage = "some-first-message",
                SystemPrompt = "some-system-prompt",
                PostHistoryInstructions = "some-post-history-instructions",
                MessageExamples = "Joe: Request\nJane: Response",
            },
            Messages =
            {
                new ChatMessageData { User = "Joe", Text = "Hello" },
                new ChatMessageData { User = "Jane", Text = "World" },
                new ChatMessageData { User = "Joe", Text = "Question" },
            },
            Actions = new[] { "action1", "action2" },
            Context = "some-context",
        }, 4096);

        Assert.That(actual, Is.EqualTo("""
        some-system-prompt
        Description of Jane: some-description
        Personality of Jane: some-personality
        Circumstances and context of the dialogue: some-scenario
        some-context
        Potential actions you will be able to do after you respond: action1, action2
        Joe: Hello
        Jane: World
        Joe: Question
        (some-post-history-instructions)
        Jane: 
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
    
    [Test]
    public void TestPromptActionInference()
    {
        var actual = _builder.BuildActionInferencePrompt(new ChatSessionData
        {
            UserName = "Joe",
            Character = new()
            {
                Name = "Jane",
                Description = "some-description",
                Personality = "some-personality",
                Scenario = "some-scenario",
                FirstMessage = "some-first-message",
                SystemPrompt = "some-system-prompt",
                PostHistoryInstructions = "some-post-history-instructions",
                MessageExamples = "Joe: Request\nJane: Response",
            },
            Messages =
            {
                new ChatMessageData { User = "Joe", Text = "Hello" },
                new ChatMessageData { User = "Jane", Text = "World" },
                new ChatMessageData { User = "Joe", Text = "Question" },
            },
            Actions = new[] { "action1", "action2" },
            Context = "some-context",
        });

        Assert.That(actual, Is.EqualTo("""
        You are tasked with inferring the best action from a list based on the content of a sample chat.

        Actions: [action1], [action2]
        Conversation Context:
        Jane's Personality: some-personality
        Scenario: some-scenario
        Context: some-context

        Conversation:
        Joe: Hello
        Jane: World
        Joe: Question
        
        Based on the last message, which of the following actions is the most applicable for Jane: [action1], [action2]

        Only write the action.

        Action: [
        """.ReplaceLineEndings("\n").TrimExcess()));
    }
}