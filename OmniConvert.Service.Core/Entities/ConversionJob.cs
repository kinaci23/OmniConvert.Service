namespace OmniConvert.Service.Core.Entities;

using OmniConvert.Service.Core.Enums;

/// <summary>Bir dönüşüm işinin tam yaşam döngüsünü temsil eder.</summary>
public class ConversionJob
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredInputPath { get; set; } = string.Empty;

    /// <summary>Oluşturma aşamasında belirlenen çıktı yolu. İşlem boyunca değişmez.</summary>
    public string StoredOutputPath { get; set; } = string.Empty;

    public SourceFormat SourceFormat { get; set; }
    public ConversionProfileKind ProfileKind { get; set; }

    // Kullanıcı override alanları — null ise preset değeri kullanılır
    public int? DpiOverride { get; set; }
    public string? ColorModeOverride { get; set; }
    public string? CompressionOverride { get; set; }

    public JobStatus Status { get; set; }
    public PipelineKind? SelectedPipeline { get; set; }
    public PipelineKind? FallbackPipeline { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public FailureCategory? FailureCategory { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? QueuedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool UsedFallback { get; set; }
    public int AttemptCount { get; set; }
}