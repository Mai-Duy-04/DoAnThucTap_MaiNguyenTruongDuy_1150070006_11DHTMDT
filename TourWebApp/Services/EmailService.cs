using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TourWebApp.Models;

namespace TourWebApp.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendPaymentSuccessEmailAsync(string toEmail, string customerName, string bookingCode, string tourName, DateTime? departureDate, decimal paidAmount)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            _logger.LogWarning("Skip sending payment email because SMTP config or recipient is missing.");
            return;
        }

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        var subject = $"[HappyTrip] Thanh toan thanh cong - {bookingCode}";
        var body = $@"Xin chao {customerName},

HappyTrip da ghi nhan thanh toan thanh cong cho don dat tour cua ban.

Ma booking: {bookingCode}
Tour: {tourName}
Ngay khoi hanh: {(departureDate.HasValue ? departureDate.Value.ToString("dd/MM/yyyy") : "Dang cap nhat")}
So tien da thanh toan: {paidAmount:N0} VND

Cam on ban da dong hanh cung HappyTrip!";

        using var message = new MailMessage(
            new MailAddress(_settings.SenderEmail, _settings.SenderName),
            new MailAddress(toEmail))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        await client.SendMailAsync(message);
    }
}
