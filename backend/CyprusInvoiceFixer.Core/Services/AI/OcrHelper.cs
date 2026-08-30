using Tesseract;

namespace CyprusInvoiceFixer.Core.Services.AI;

public static class OcrHelper
{
    private static readonly string TessDataPath =
        Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? "./tessdata";

    public static string ExtractText(byte[] imageBytes)
    {
        try
        {
            using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);
            return page.GetText();
        }
        catch (Exception ex)
        {
            return $"[OCR failed: {ex.Message}]";
        }
    }
}
