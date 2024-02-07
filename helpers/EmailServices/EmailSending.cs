using System.Net.Mail;
using System.Net;
using KEOPBackend.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Data;
using System.Numerics;

namespace KEOPBackend.helpers.EmailServices
{
    public class EmailSending
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var mail = "yrsaikrishna@gmail.com";
            var pass = "jgvgaulefghziapo";

            var mailMessage = new MailMessage();
            mailMessage.To.Add(new MailAddress(email));
            mailMessage.From = new MailAddress(mail, "Keep Everything at One Place Admin");
            mailMessage.ReplyTo = new MailAddress(mail, "Keep Everything at One Place Admin");
            mailMessage.Subject = subject;
            mailMessage.Body = message;
            mailMessage.IsBodyHtml = true;

            var client = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(mail, pass)
            };
            client.UseDefaultCredentials = false;

            return client.SendMailAsync(mailMessage);
        }

        public string AccountCreationTemplate(string email, string pass)
        {
            var template = $@"
                    <html>
                    <head>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f2f2f2;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                            background-color: #fff;
                            border-radius: 5px;
                            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
                        }}
                        .header {{
                            background-color: #007BFF;
                            color: #fff;
                            text-align: center;
                            padding: 10px;
                            border-radius: 5px 5px 0 0;
                        }}
                        .content {{
                            padding: 20px;
                        }}
                        .button {{
                            display: inline-block;
                            padding: 10px 20px;
                            background-color: #007BFF;
                            color: #fff;
                            text-decoration: none;
                            border-radius: 5px;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Welcome to Keep Everything at One Place Website</h2>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>Your account has been created successfully.</p>
                            <p>Here are your login credentials:</p>
                            <p>Email: <strong>{email}</strong></p>
                            <p>Password: <strong>{pass}</strong></p>
                            <p>You can now log in using your email and password.</p>
                        </div>
                    </div>
                </body>
                </html>";
            return template;
        }

        public string UserEditAlertMsgTemplate(string title, string date, string time, string link)
        {
            string subject = "UPDATED EVENT REMAINDER";
            string template = $@"<!DOCTYPE html>
                        <html>
                        <head>
                            <title>{ subject}</title>
                        </head>
                        <body style='font-family: Arial, sans-serif;'>
                            <table width='100%' cellpadding='10'>
                                <tr style='background-color: #007bff; color: #fff;'>
                                    <td colspan='2' align='center'>
                                        <h1>" + subject + " FOR " + title + @"</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan='2'>
                                        <h3>Dear User,</h3>
                                        <p>We hope this message finds you well.</p>
                                        <p>Your event details are as follows:</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>Title:</strong></td>
                                    <td>
                                        <p>" + title + @"</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>Updated Date:</strong></td>
                                    <td>
                                        <p>" + date + @"</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td><strong>Updated Time:</strong></td>
                                    <td>
                                        <p>" + time + @"</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan='2' style='background-color: #f2f2f2; padding: 5px;'><strong>Would you like to schedule this event in your calendar? </strong>
                                        <a href=" + link + @">Click here to add into your Calendar</a>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan='2' style='background-color: #f2f2f2; padding: 10px;'>
                                        <p><strong>Alert:</strong> Don't forget about your upcoming event!</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan='2' align='center'>
                                        <p>Thank you for using our service. If you have any questions or need further assistance, please don't hesitate to contact us.</p>
                                        <p>Best regards,</p>
                                        <p>Your Service Provider</p>
                                    </td>
                                </tr>
                            </table>
                        </body>
                        </html>";
            return template;
        }

        public string UserAlertMsgTemplate(string title, string date, string time, string link)
        {
            string subject = "EVENT REMAINDER";
            string template = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>{subject}</title>
            </head>
            <body style='font-family: Arial, sans-serif;'>
                <table width='100%' cellpadding='10'>
                    <tr style='background-color: #007bff; color: #fff;'>
                        <td colspan='2' align='center'>
                            <h1>{subject} FOR {title}</h1>
                        </td>
                    </tr>
                    <tr>
                        <td colspan='2'>
                            <h3>Dear User,</h3>
                            <p>We hope this message finds you well.</p>
                            <p>Your event details are as follows:</p>
                        </td>
                    </tr>
                    <tr>
                        <td><strong>Title:</strong></td>
                        <td><p>{title}</p></td>
                    </tr>
                    <tr>
                        <td><strong>Date:</strong></td>
                        <td><p>{date}</p></td>
                    </tr>
                    <tr>
                        <td><strong>Time:</strong></td>
                        <td><p>{time}</p></td>
                    </tr>
                    <tr>
                        <td colspan='2' style='background-color: #f2f2f2; padding: 5px;'><strong>Would you like to schedule this event in your calendar? </strong>
                            <a href='{link}'>Click here to add into your Calendar</a>
                        </td>
                    </tr>
                    <tr>
                        <td colspan='2' style='background-color: #f2f2f2; padding: 10px;'>
                            <p><strong>Alert:</strong> Don't forget about your upcoming event!</p>
                        </td>
                    </tr>
                    <tr>
                        <td colspan='2' align='center'>
                            <p>Thank you for using our service. If you have any questions or need further assistance, please don't hesitate to contact us.</p>
                            <p>Best regards,</p>
                            <p>Your Service Provider</p>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";
            return template;
        }
    }
}
