namespace OmniConvert.Service.Core.Enums;

/// <summary>
/// Domain format ailelerini temsil eder.
/// Uzantı → format eşlemesi Application katmanındaki DetectFormat içinde yapılır.
/// Jpg/Jpeg → Jpeg, Tif/Tiff → Tiff olarak birleştirildi.
/// </summary>
public enum SourceFormat
{
    Unknown = 0,
    Docx = 1,
    Xlsx = 2,
    Pdf = 3,
    Jpeg = 4,
    Png = 5,
    Tiff = 6
}