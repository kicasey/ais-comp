using System.Net.Http.Headers;

namespace API_For_Server.Services;

public interface IWebhookProxyService
{
    Task<HttpResponseMessage> ForwardToChatAsync(HttpRequest request, CancellationToken ct = default);
    Task<HttpResponseMessage> ForwardToEmployeeChatAsync(HttpRequest request, CancellationToken ct = default);
    Task<HttpResponseMessage> ForwardToResumesAsync(HttpRequest request, CancellationToken ct = default);
}

public class WebhookProxyService : IWebhookProxyService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookProxyService> _logger;

    private static readonly HashSet<string> PdfConvertibleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".odt", ".rtf"
    };

    private static readonly HashSet<string> PdfConvertibleMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.oasis.opendocument.text",
        "application/rtf"
    };

    public WebhookProxyService(IHttpClientFactory clientFactory, IConfiguration config, ILogger<WebhookProxyService> logger)
    {
        _clientFactory = clientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> ForwardToChatAsync(HttpRequest request, CancellationToken ct = default)
    {
        var url = _config["Webhooks:Chat"];
        if (string.IsNullOrEmpty(url)) throw new InvalidOperationException("Webhooks:Chat not configured.");
        return await ForwardRequestAsync(url, request, isMultipart: false, ct);
    }

    public async Task<HttpResponseMessage> ForwardToEmployeeChatAsync(HttpRequest request, CancellationToken ct = default)
    {
        var url = _config["Webhooks:EmployeeChat"];
        if (string.IsNullOrEmpty(url)) throw new InvalidOperationException("Webhooks:EmployeeChat not configured.");
        return await ForwardRequestAsync(url, request, isMultipart: false, ct);
    }

    public async Task<HttpResponseMessage> ForwardToResumesAsync(HttpRequest request, CancellationToken ct = default)
    {
        var url = _config["Webhooks:Resumes"];
        if (string.IsNullOrEmpty(url)) throw new InvalidOperationException("Webhooks:Resumes not configured.");
        return await ForwardRequestAsync(url, request, isMultipart: true, ct);
    }

    private async Task<HttpResponseMessage> ForwardRequestAsync(string webhookUrl, HttpRequest request, bool isMultipart, CancellationToken ct)
    {
        var client = _clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(isMultipart ? 120 : 30);

        if (isMultipart && request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(ct);
            using var content = new MultipartFormDataContent();
            foreach (var file in form.Files)
            {
                var ext = Path.GetExtension(file.FileName);
                var needsConversion = !string.IsNullOrEmpty(ext)
                    && PdfConvertibleExtensions.Contains(ext)
                    || PdfConvertibleMimeTypes.Contains(file.ContentType ?? "");

                if (needsConversion)
                {
                    _logger.LogInformation("Converting {FileName} ({ContentType}) to PDF via Gotenberg", file.FileName, file.ContentType);
                    var pdfBytes = await ConvertToPdfAsync(file, ct);
                    if (pdfBytes != null)
                    {
                        var pdfFileName = Path.GetFileNameWithoutExtension(file.FileName) + ".pdf";
                        var streamContent = new ByteArrayContent(pdfBytes);
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                        content.Add(streamContent, file.Name, pdfFileName);
                        continue;
                    }
                    _logger.LogWarning("Gotenberg conversion failed for {FileName}, sending original", file.FileName);
                }

                var originalStream = file.OpenReadStream();
                var originalContent = new StreamContent(originalStream);
                originalContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                content.Add(originalContent, file.Name, file.FileName);
            }
            foreach (var key in form.Keys.Where(k => form[k].Count > 0))
            {
                content.Add(new StringContent(form[key].ToString()!), key);
            }
            return await client.PostAsync(webhookUrl, content, ct);
        }

        // JSON or other body
        using var req = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
        if (request.ContentLength.HasValue && request.ContentLength > 0)
        {
            req.Content = new StreamContent(request.Body);
            if (request.ContentType != null)
                req.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
        }
        return await client.SendAsync(req, ct);
    }

    private async Task<byte[]?> ConvertToPdfAsync(IFormFile file, CancellationToken ct)
    {
        var gotenbergUrl = _config["Gotenberg:BaseUrl"];
        if (string.IsNullOrEmpty(gotenbergUrl)) return null;

        try
        {
            var client = _clientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            using var convertContent = new MultipartFormDataContent();
            var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            convertContent.Add(streamContent, "files", file.FileName);

            var response = await client.PostAsync($"{gotenbergUrl.TrimEnd('/')}/forms/libreoffice/convert", convertContent, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gotenberg returned {StatusCode} for {FileName}", response.StatusCode, file.FileName);
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gotenberg conversion error for {FileName}", file.FileName);
            return null;
        }
    }
}
