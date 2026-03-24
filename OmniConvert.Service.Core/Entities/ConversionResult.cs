namespace OmniConvert.Service.Core.Entities;

using OmniConvert.Service.Core.Enums;

/// <summary>Orchestrator'ın dönüşüm sonucunu döndürmek için kullandığı geçici model.</summary>
public class ConversionResult
{
    public Guid JobId { get; set; }
    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Başarılı sonuçlarda null.</summary>
    public FailureCategory? FailureCategory { get; set; }
    public bool UsedFallback { get; set; }

    /// <summary>Hangi pipeline kullanıldı. Seçim yapılamadan başarısız olduysa null.</summary>
    public PipelineKind? PipelineUsed { get; set; }
}