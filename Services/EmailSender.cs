using Microsoft.Extensions.Logging;

// Email delivery abstraction. Registration/verification and password-reset use this so
// the flow is testable without a live mail server. Swap LoggingEmailSender for an SMTP
// implementation (config) in production without touching the controllers.
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}

// Dev implementation: writes the message (including any verification/reset link) to the
// application log so it can be copied from the console during testing.
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        _logger.LogInformation(
            "\n===== DEV EMAIL =====\nTo: {To}\nSubject: {Subject}\n{Body}\n=====================",
            toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
