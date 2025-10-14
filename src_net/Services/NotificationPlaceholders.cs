// NotificationPlaceholders.cs
// This file contains placeholder methods showing how to integrate SendGrid and Twilio.
// DO NOT store API keys in source code for production. Use Azure Key Vault / App Settings.

using System.Threading.Tasks;

namespace MembershipAppBEAPI.Services
{
    public static class NotificationPlaceholders
    {
        // Example SendGrid usage (commented)
        // public static async Task SendBirthdayEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
        // {
        //     var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        //     var client = new SendGrid.SendGridClient(apiKey);
        //     var from = new SendGrid.Helpers.Mail.EmailAddress("noreply@yourchurch.org", "CCC Redemption");
        //     var to = new SendGrid.Helpers.Mail.EmailAddress(toEmail);
        //     var msg = SendGrid.Helpers.Mail.MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        //     var response = await client.SendEmailAsync(msg);
        // }

        // Example Twilio usage (commented)
        // public static void SendSms(string toPhone, string body)
        // {
        //     var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        //     var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        //     Twilio.TwilioClient.Init(accountSid, authToken);
        //     var message = Twilio.Rest.Api.V2010.Account.MessageResource.Create(
        //         body: body,
        //         from: new Twilio.Types.PhoneNumber(Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER")),
        //         to: new Twilio.Types.PhoneNumber(toPhone)
        //     );
        // }
    }
}
