namespace CVMatch.Web.Services;

public interface IEmailSender
{
    Task SendAsync(string aliciEposta, string konu, string htmlIcerik, CancellationToken ct = default);
}