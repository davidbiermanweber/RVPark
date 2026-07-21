using SendGrid;
using SendGrid.Helpers.Mail;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task SendOrderConfirmationAsync(string toEmail, int orderId, decimal amount)
    {
        var client = new SendGridClient(_config["SendGrid:ApiKey"]);
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_config["SendGrid:From"], _config["SendGrid:FromName"]),
            Subject = $"Order Confirmation #{orderId}",
            PlainTextContent = $"Your payment of ${amount:F2} has been processed.\nOrder #{orderId} is confirmed.\n\nThank you for choosing RV Park!"
        };
        msg.AddTo(new EmailAddress(toEmail));
        await client.SendEmailAsync(msg);
    }
}