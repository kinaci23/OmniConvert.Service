namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.ValueObjects;

public interface IConversionPipeline
{
    PipelineKind Kind { get; }

    bool CanHandle(SourceFormat format);

    Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default);
}