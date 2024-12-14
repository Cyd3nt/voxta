using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Services.LMStudio;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class LMStudioSettingsViewModel : RemoteLLMServiceSettingsViewModelBase<LMStudioParameters>
{
    public LMStudioSettingsViewModel()
    {
    }
    
    [SetsRequiredMembers]
    public LMStudioSettingsViewModel(ConfiguredService<LMStudioSettings> source)
        : base(source, source.Settings)
    {
    }

    public ConfiguredService<LMStudioSettings> ToSettings(Guid serviceId)
    {
        return new ConfiguredService<LMStudioSettings>
        {
            Id = serviceId,
            ServiceName = LMStudioConstants.ServiceName,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
            Enabled = Enabled,
            Settings = new LMStudioSettings
            {
                Uri = Uri.TrimCopyPasteArtefacts(),
                PromptFormat = PromptFormat,
                MaxContextTokens = MaxContextTokens,
                MaxMemoryTokens = MaxMemoryTokens,
                SummaryMaxTokens = SummaryMaxTokens,
                SummarizationDigestTokens = SummarizationDigestTokens,
                SummarizationTriggerTokens = SummarizationTriggerTokens,
                Parameters = GetParameters<LMStudioParameters>(),
            }
        };
    }
}