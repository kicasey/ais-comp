namespace TalentStrategyAI.API.Services;

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
            try { return System.IO.File.ReadAllText(fullPath); }
            catch { return null; }
        }
        return null;
    }
}
