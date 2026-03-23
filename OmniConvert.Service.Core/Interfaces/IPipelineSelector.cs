namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.ValueObjects;

public interface IPipelineSelector
{
    PipelineSelectionResult Select(SourceFormat format);
}