using Microsoft.AspNetCore.Identity.UI.Services;

namespace CVMatch.Web.Services;

public class IdentityEmailSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
{
    private readonly Services.IEmailSender _sender;

    public IdentityEmailSender(Services.IEmailSender sender) => _sender = sender;

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
        => _sender.SendAsync(email, subject, htmlMessage);
}