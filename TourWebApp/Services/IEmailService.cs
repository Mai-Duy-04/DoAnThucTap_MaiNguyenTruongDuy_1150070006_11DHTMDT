namespace TourWebApp.Services;

public interface IEmailService
{
    Task SendPaymentSuccessEmailAsync(string toEmail, string customerName, string bookingCode, string tourName, DateTime? departureDate, decimal paidAmount);
}
