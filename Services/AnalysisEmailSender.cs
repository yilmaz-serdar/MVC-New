using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using DependencyAnalyzer.Mvc.Models;

namespace DependencyAnalyzer.Mvc.Services;

public sealed class AnalysisEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    public bool IsPickup => _configuration["Email:Mode"] != "Smtp";
    public AnalysisEmailSender(IConfiguration configuration, IWebHostEnvironment environment)
    { _configuration = configuration; _environment = environment; }

    public async Task SendAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Email:PublicBaseUrl"] ?? "http://localhost:5090";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new InvalidOperationException("Email:PublicBaseUrl must be an absolute HTTP(S) URL.");
        using var message = new MailMessage(_configuration["Email:From"] ?? "analysis@abc.com", job.Request.Email);
        message.Subject = $"Servis analizi tamamlandı: {job.Request.ServiceName}";
        message.Body = $"Servis: {job.Request.ServiceName}\nAnaliz süresi: Son {job.Request.Days} gün\nSonuç: {baseUrl.TrimEnd('/')}/Analysis/Analyze/{job.Id}\n\nAnaliz yanıtı JSON olarak ektedir.\nİletişim ve destek: support@abc.com";
        message.BodyEncoding = Encoding.UTF8;
        message.SubjectEncoding = Encoding.UTF8;
        message.Attachments.Add(Attachment.CreateAttachmentFromString(JsonSerializer.Serialize(job.Result!.Topology), "analysis-result.json", Encoding.UTF8, "application/json"));
        using var client = new SmtpClient();
        if (IsPickup)
        {
            var directory = Path.Combine(_environment.ContentRootPath, "App_Data", "Mail");
            Directory.CreateDirectory(directory);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = directory;
        }
        else
        {
            client.Host = _configuration["Email:Host"] ?? throw new InvalidOperationException("SMTP host is missing.");
            client.Port = _configuration.GetValue("Email:Port", 587);
            client.EnableSsl = _configuration.GetValue("Email:EnableSsl", true);
            var user = _configuration["Email:Username"];
            if (!string.IsNullOrEmpty(user)) client.Credentials = new NetworkCredential(user, _configuration["Email:Password"]);
        }
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message).WaitAsync(cancellationToken);
    }
}
