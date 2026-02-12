namespace TalentStrategyAI.API.Services;

/// <summary>Extracts plain text from stored resume files for AI/API use. .txt supported; PDF/DOCX return null until a library is added.</summary>
public interface IResumeTextService
{
    string? GetResumeText(string contentRootPath, string relativePath);
}

public class ResumeTextService : IResumeTextService
{
    public string? GetResumeText(string contentRootPath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        var fullPath = Path.Combine(contentRootPath, "Uploads", relativePath.Replace('\\', '/').TrimStart('/'));
        if (!System.IO.File.Exists(fullPath)) return null;
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        if (ext == ".txt")
        {
            try { return System.IO.File.ReadAllText(fullPath); } catch { return null; }
        }
        // PDF/DOCX: could add PdfPig + OpenXml later for full extraction
        return null;
    }
}
