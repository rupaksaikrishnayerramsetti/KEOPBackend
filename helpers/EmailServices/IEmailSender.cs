namespace KEOPBackend.helpers.EmailServices
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string msg);
    }
}
