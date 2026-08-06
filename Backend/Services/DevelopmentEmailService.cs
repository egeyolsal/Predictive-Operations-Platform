namespace TaskInventoryApi.Services;

public class DevelopmentEmailService : IEmailService
{
    private readonly ILogger<DevelopmentEmailService> _logger;

    public DevelopmentEmailService(ILogger<DevelopmentEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        // In a real application, you would use SMTP, SendGrid, etc.
        // For development, we just log the token to the console.
        var resetLink = $"http://localhost:4200/reset-password?token={resetToken}";
        
        _logger.LogInformation("\n=======================================================\n" +
                               "EMAIL SENT TO: {Email}\n" +
                               "SUBJECT: Password Reset Request\n" +
                               "BODY: Please click the link below to reset your password:\n" +
                               "{Link}\n" +
                               "=======================================================\n", 
                               toEmail, resetLink);
                               
        return Task.CompletedTask;
    }
}
