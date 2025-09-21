using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Interfaces;
using DotNetEnv;
using MailKit.Net.Smtp;
using MimeKit;

namespace api.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendWelcomeEmailAsync(string email, string username)
        {
            Env.Load();
            var emailSender = Environment.GetEnvironmentVariable("email");
            var password = Environment.GetEnvironmentVariable("password");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Babel E-Library", emailSender));
            message.To.Add(new MailboxAddress(username, email));
            message.Subject = "Welcome to Babel! 🎉";

            string body = $@"
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background: #f0f0f0; }}
        .container {{ max-width: 650px; margin: 20px auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #2c3e50 0%, #3498db 100%); padding: 30px 20px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 300; }}
        .header .icon {{ font-size: 48px; margin-bottom: 10px; display: block; }}
        .content {{ padding: 30px; }}
        .greeting {{ font-size: 20px; color: #2c3e50; margin-bottom: 20px; font-weight: 300; }}
        .section {{ margin-bottom: 25px; font-size: 16px; color: #555; }}
        .cta {{ text-align: center; margin: 30px 0; }}
        .cta a {{ display: inline-block; background: #3498db; color: white; padding: 12px 25px; border-radius: 8px; text-decoration: none; font-weight: 500; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #666; font-size: 12px; border-top: 1px solid #eee; }}
        .logo {{ font-weight: bold; color: #3498db; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <span class='icon'>📚</span>
            <h1>Babel E-Library</h1>
            <p style='margin: 5px 0 0 0; opacity: 0.9; font-size: 16px;'>Your literary adventure starts here!</p>
        </div>

        <div class='content'>
            <div class='greeting'>Hello {username}! 👋</div>

            <div class='section'>
                Welcome to <strong>Babel E-Library</strong>! We're thrilled to have you join our community of passionate readers.
                Explore thousands of books, track your borrows, and discover your next favorite story.
            </div>

            <div class='section'>
                Here are a few tips to get you started:
                <ul>
                    <li>Browse the <strong>Collection</strong> to discover books by genre or author.</li>
                    <li>Add favorites to keep track of what you love.</li>
                    <li>Check your <strong>Borrows</strong> to manage your current and past books.</li>
                </ul>
            </div>

            <div class='cta'>
                <a href=''>Go to Your Dashboard</a>
            </div>
        </div>

        <div class='footer'>
            <p><span class='logo'>Babel E-Library</span> • {DateTime.Now:MMM dd, yyyy}</p>
            <p style='margin: 5px 0; font-style: italic;'>&quot;A room without books is like a body without a soul.&quot; - Cicero</p>
        </div>
    </div>
</body>
</html>";

            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(emailSender, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }


        public async Task SendReportEmailAsync(string email, string username, string tempPath)
        {
            Env.Load();
            var emailSender = Environment.GetEnvironmentVariable("email");
            var password = Environment.GetEnvironmentVariable("password");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Babel E-Library", emailSender));
            message.To.Add(new MailboxAddress(username, email));
            message.Subject = "Your Weekly Library Report 📚";

            var body = $@"
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }}
        .container {{ max-width: 650px; margin: 20px auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.2); }}
        .header {{ background: linear-gradient(135deg, #2c3e50 0%, #3498db 100%); padding: 30px 20px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 300; }}
        .header .icon {{ font-size: 48px; margin-bottom: 10px; display: block; }}
        .content {{ padding: 30px; }}
        .greeting {{ font-size: 20px; color: #2c3e50; margin-bottom: 20px; font-weight: 300; }}
        .section {{ margin-bottom: 25px; }}
        .feature-list {{ background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 20px 0; }}
        .feature-item {{ display: flex; align-items: center; margin: 12px 0; padding: 8px 0; }}
        .feature-icon {{ font-size: 20px; margin-right: 15px; width: 30px; }}
        .feature-text {{ color: #555; font-size: 16px; }}
        .stats-card {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 12px; padding: 20px; text-align: center; margin: 20px 0; }}
        .stats-text {{ font-size: 16px; margin: 0; opacity: 0.9; }}
        .cta-section {{ background: #e8f4fd; border-radius: 12px; padding: 25px; text-align: center; margin: 20px 0; }}
        .cta-text {{ color: #2c3e50; font-size: 16px; margin-bottom: 15px; }}
        .tips {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px 20px; margin: 20px 0; border-radius: 0 8px 8px 0; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #666; font-size: 12px; border-top: 1px solid #eee; }}
        .logo {{ font-weight: bold; color: #3498db; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <span class='icon'>📚</span>
            <h1>Babel E-Library</h1>
            <p style='margin: 5px 0 0 0; opacity: 0.9; font-size: 16px;'>Your Weekly Reading Journey</p>
        </div>
        
        <div class='content'>
            <div class='greeting'>Hello {username}! 👋</div>
            
            <div class='section'>
                <p style='font-size: 16px; color: #555; margin-bottom: 20px;'>
                    Your personalized library report is ready! We've compiled your borrowing activity, 
                    reading progress, and important reminders to help you stay on track with your literary adventures.
                </p>
            </div>

            <div class='stats-card'>
                <p class='stats-text'>📖 Your Reading Dashboard Includes:</p>
            </div>

            <div class='feature-list'>
                <div class='feature-item'>
                    <span class='feature-icon'>⚠️</span>
                    <span class='feature-text'>Overdue books requiring immediate attention</span>
                </div>
                <div class='feature-item'>
                    <span class='feature-icon'>📖</span>
                    <span class='feature-text'>Currently borrowed books and due dates</span>
                </div>
                <div class='feature-item'>
                    <span class='feature-icon'>🔄</span>
                    <span class='feature-text'>Pending return requests status</span>
                </div>
                <div class='feature-item'>
                    <span class='feature-icon'>✅</span>
                    <span class='feature-text'>Recently returned books summary</span>
                </div>
                <div class='feature-item'>
                    <span class='feature-icon'>📊</span>
                    <span class='feature-text'>Visual overview of your reading habits</span>
                </div>
            </div>

            <div class='cta-section'>
                <p class='cta-text'>
                    <strong>📎 Your detailed report is attached!</strong><br>
                    Open the PDF to see your complete library activity and personalized recommendations.
                </p>
            </div>

            <div class='tips'>
                <strong>💡 Pro Tip:</strong> Return books on time to avoid late fees and help other readers access popular titles. 
                Set calendar reminders for due dates to stay organized!
            </div>

            <div class='section' style='text-align: center; margin-top: 30px;'>
                <p style='color: #666; font-size: 14px; margin-bottom: 10px;'>
                    Questions about your account or need help finding your next great read?
                </p>
                <p style='color: #3498db; font-size: 16px; font-weight: 500;'>
                    Visit us online or contact our friendly librarians! 📞
                </p>
            </div>
        </div>

        <div class='footer'>
            <p style='margin: 5px 0;'>
                <span class='logo'>Babel E-Library</span> • Generated on {DateTime.Now:MMM dd, yyyy} at {DateTime.Now:HH:mm}
            </p>
            <p style='margin: 5px 0; font-style: italic;'>
                &quot;A room without books is like a body without a soul.&quot; - Cicero
            </p>
            <p style='margin: 10px 0 0 0; color: #999;'>
                Keep reading, keep growing! 🌱
            </p>
        </div>
    </div>
</body>
</html>";

            var multipart = new Multipart("mixed")
    {
        new TextPart("html") { Text = body }
    };

            if (File.Exists(tempPath))
            {
                // Read file into memory first to avoid file lock issues
                var pdfBytes = await File.ReadAllBytesAsync(tempPath);
                multipart.Add(new MimePart("application", "pdf")
                {
                    Content = new MimeContent(new MemoryStream(pdfBytes), ContentEncoding.Default),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = Path.GetFileName(tempPath)
                });
            }

            message.Body = multipart;

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(emailSender, password);
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);

                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}