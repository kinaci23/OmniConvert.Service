namespace OmniConvert.Service.Conversion.Pipelines.Word;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniConvert.Service.Conversion.Configuration;
using OmniConvert.Service.Conversion.Pipelines.Pdf;
using OmniConvert.Service.Core.Enums;
using OmniConvert.Service.Core.Interfaces;
using OmniConvert.Service.Core.ValueObjects;

public sealed class LibreOfficeWordPdfBridgePipeline : IConversionPipeline
{
    /// <summary>
    /// Benchmark: candidate path fallback mantığı korunuyor.
    /// Config boşsa veya dosya yoksa bu sırayla denenir.
    /// </summary>
    private static readonly string[] CandidatePaths =
    [
        @"C:\Program Files\LibreOffice\program\soffice.exe",
        @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
    ];

    private readonly IExternalProcessRunner _processRunner;
    private readonly GhostscriptScaledPipeline _ghostscriptPipeline;
    private readonly LibreOfficeOptions _options;
    private readonly ILogger<LibreOfficeWordPdfBridgePipeline> _logger;

    public LibreOfficeWordPdfBridgePipeline(
        IExternalProcessRunner processRunner,
        GhostscriptScaledPipeline ghostscriptPipeline,
        IOptions<LibreOfficeOptions> options,
        ILogger<LibreOfficeWordPdfBridgePipeline> logger)
    {
        _processRunner = processRunner;
        _ghostscriptPipeline = ghostscriptPipeline;
        _options = options.Value;
        _logger = logger;
    }

    public PipelineKind Kind => PipelineKind.LibreOfficeWordPdfBridge;

    public bool CanHandle(SourceFormat format) => format == SourceFormat.Docx;

    public async Task<PipelineExecutionResult> ExecuteAsync(
        ConversionContext context,
        CancellationToken cancellationToken = default)
    {
        // Sadece .docx kabul ediliyor — benchmark davranışı
        var ext = Path.GetExtension(context.InputFilePath);
        if (!string.Equals(ext, ".docx", StringComparison.OrdinalIgnoreCase))
        {
            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: $"LibreOfficeWordPdfBridge sadece .docx destekler. Gelen: {ext}",
                FailureCategory: FailureCategory.Validation);
        }

        var libreOfficePath = ResolveLibreOfficePath();

        if (!File.Exists(libreOfficePath))
        {
            _logger.LogError("LibreOffice executable bulunamadı: {Path}", libreOfficePath);
            return new PipelineExecutionResult(
                Success: false,
                OutputPath: null,
                ErrorMessage: $"LibreOffice executable bulunamadı: {libreOfficePath}",
                FailureCategory: FailureCategory.ExternalProcess);
        }

        // Her çalışma için izole temp dizin — context.WorkspacePath altında
        var runWorkDir = Path.Combine(
            context.WorkspacePath,
            $"lo_{Path.GetFileNameWithoutExtension(context.InputFilePath)}_{Guid.NewGuid():N}");

        Directory.CreateDirectory(runWorkDir);

        // LibreOffice profil dizini — paralel çalışmalarda çakışmayı önler
        var tempProfilePath = Path.Combine(runWorkDir, "lo-profile");
        Directory.CreateDirectory(tempProfilePath);

        // LibreOffice output'u: input dosya adı + .pdf
        var tempPdfPath = Path.Combine(
            runWorkDir,
            $"{Path.GetFileNameWithoutExtension(context.InputFilePath)}.pdf");

        try
        {
            var tempProfileUri = new Uri(tempProfilePath).AbsoluteUri;

            // Benchmark CLI argümanları birebir korunuyor
            var arguments =
                $"--headless " +
                $"--nologo " +
                $"--norestore " +
                $"--nolockcheck " +
                $"--nodefault " +
                $"--convert-to pdf " +
                $"--outdir \"{runWorkDir}\" " +
                $"\"{context.InputFilePath}\" " +
                $"-env:UserInstallation={tempProfileUri}";

            _logger.LogInformation(
                "LibreOffice başlatılıyor | Job: {JobId} | Input: {Input} | " +
                "TempPdf: {TempPdf} | Output: {Output} | ExePath: {Exe}",
                context.JobId, context.InputFilePath,
                tempPdfPath, context.OutputFilePath, libreOfficePath);

            if (File.Exists(tempPdfPath))
                File.Delete(tempPdfPath);

            cancellationToken.ThrowIfCancellationRequested();

            ExternalProcessResult loResult;
            try
            {
                loResult = await _processRunner.RunAsync(
                    executable: libreOfficePath,
                    arguments: arguments,
                    workingDirectory: runWorkDir,
                    timeoutSeconds: _options.TimeoutSeconds,
                    cancellationToken: cancellationToken);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(
                    "LibreOffice timeout. Job: {JobId} | Limit: {Timeout}s",
                    context.JobId, _options.TimeoutSeconds);

                return new PipelineExecutionResult(
                    Success: false,
                    OutputPath: null,
                    ErrorMessage: ex.Message,
                    FailureCategory: FailureCategory.Timeout);
            }

            if (loResult.ExitCode != 0)
            {
                _logger.LogError(
                    "LibreOffice başarısız | Job: {JobId} | ExitCode: {Code} | Stderr: {Stderr}",
                    context.JobId, loResult.ExitCode, loResult.StandardError);

                return new PipelineExecutionResult(
                    Success: false,
                    OutputPath: null,
                    ErrorMessage: $"LibreOffice exit code {loResult.ExitCode}: {loResult.StandardError}",
                    FailureCategory: FailureCategory.ExternalProcess);
            }

            if (!File.Exists(tempPdfPath))
            {
                _logger.LogError(
                    "LibreOffice başarılı döndü ama PDF oluşmadı | Job: {JobId} | Beklenen: {Path}",
                    context.JobId, tempPdfPath);

                return new PipelineExecutionResult(
                    Success: false,
                    OutputPath: null,
                    ErrorMessage: $"LibreOffice PDF üretemedi: {tempPdfPath}",
                    FailureCategory: FailureCategory.Conversion);
            }

            _logger.LogInformation(
                "LibreOffice PDF üretildi | Job: {JobId} | Boyut: {Size} bytes",
                context.JobId, new FileInfo(tempPdfPath).Length);

            _logger.LogInformation(
                "Ghostscript bridge başlatılıyor | Job: {JobId}", context.JobId);

            // PDF → TIFF: mevcut GhostscriptScaledPipeline aynı profil ile çalışıyor
            var gsContext = new ConversionContext(
                JobId: context.JobId,
                InputFilePath: tempPdfPath,
                OutputFilePath: context.OutputFilePath,
                WorkspacePath: runWorkDir,
                SourceFormat: SourceFormat.Pdf,
                Profile: context.Profile);

            var gsResult = await _ghostscriptPipeline.ExecuteAsync(gsContext, cancellationToken);

            if (!gsResult.Success)
            {
                _logger.LogError(
                    "Ghostscript bridge başarısız | Job: {JobId} | Hata: {Error}",
                    context.JobId, gsResult.ErrorMessage);

                return gsResult;
            }

            _logger.LogInformation(
                "LibreOfficeWordPdfBridge tamamlandı | Job: {JobId} | Output: {Output}",
                context.JobId, gsResult.OutputPath);

            return gsResult;
        }
        finally
        {
            // Temp çalışma dizini her durumda temizleniyor
            if (Directory.Exists(runWorkDir))
            {
                try
                {
                    Directory.Delete(runWorkDir, recursive: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Temp dizin temizlenemedi | Job: {JobId} | Path: {Path} | Hata: {Error}",
                        context.JobId, runWorkDir, ex.Message);
                }
            }
        }
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Config öncelikli, yoksa benchmark candidate path fallback.
    /// </summary>
    private string ResolveLibreOfficePath()
    {
        // Config'te açıkça belirtilmişse direkt kullan — candidate fallback yok
        if (!string.IsNullOrWhiteSpace(_options.Path))
            return _options.Path;

        // Config boşsa candidate path'leri dene
        foreach (var candidate in CandidatePaths)
            if (File.Exists(candidate)) return candidate;

        return CandidatePaths[0];
    }
}