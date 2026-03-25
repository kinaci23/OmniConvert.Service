namespace OmniConvert.Service.Core.Interfaces;

using OmniConvert.Service.Core.Enums;

/// <summary>
/// Pipeline bazlı concurrency slot yöneticisi.
/// Her pipeline tipi için eş zamanlı çalışabilecek maksimum iş sayısını kontrol eder.
/// </summary>
public interface IConcurrencyLimiter
{
    /// <summary>
    /// Belirtilen pipeline için slot alır.
    /// Dönen IDisposable dispose edildiğinde slot serbest bırakılır.
    /// </summary>
    Task<IDisposable> AcquireAsync(
        PipelineKind pipeline,
        CancellationToken cancellationToken = default);
}