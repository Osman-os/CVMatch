using System.Net;
using System.Net.Mail;

namespace CVMatch.Web.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly bool _sslKullan;
    private readonly string _kullanici;
    private readonly string _parola;
    private readonly string _gonderenEposta;
    private readonly string _gonderenAd;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _logger = logger;

        _host = config["Smtp:Host"]
            ?? throw new InvalidOperationException("Smtp:Host yapılandırılmamış.");
        _port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        _sslKullan = !bool.TryParse(config["Smtp:UseSsl"], out var ssl) || ssl;
        _kullanici = config["Smtp:User"] ?? string.Empty;
        _parola = config["Smtp:Password"] ?? string.Empty;
        _gonderenEposta = config["Smtp:FromEmail"] ?? _kullanici;
        _gonderenAd = config["Smtp:FromName"] ?? "CVMatch";
    }

    public async Task SendAsync(
        string aliciEposta, string konu, string htmlIcerik, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = _sslKullan,
                Credentials = new NetworkCredential(_kullanici, _parola)
            };

            using var mesaj = new MailMessage
            {
                From = new MailAddress(_gonderenEposta, _gonderenAd),
                Subject = konu,
                Body = htmlIcerik,
                IsBodyHtml = true
            };

            mesaj.To.Add(aliciEposta);

            await client.SendMailAsync(mesaj, ct);

            _logger.LogInformation("E-posta gönderildi. Konu: {Konu}", konu);
        }
        catch (Exception ex)
        {
            // Alıcı adresi loglanmaz; kişisel veri
            _logger.LogError(ex, "E-posta gönderilemedi. Konu: {Konu}", konu);
        }
    }
}