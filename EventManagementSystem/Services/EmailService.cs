using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using EventManagementSystem.Models;
using Microsoft.Extensions.Logging;

namespace EventManagementSystem.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private static readonly Dictionary<string, (string OTP, DateTime ExpiryTime)> _otpStorage = new();
        private static readonly Dictionary<string, (string OTP, DateTime ExpiryTime)> _forgotPasswordOtpStorage = new();

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            
            var mail = new MailMessage()
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(toEmail));


            using var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
            {
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.Password),
                EnableSsl = true
            };

            try
            {
                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string GenerateOTP()
        {
            Random random = new Random();
            string otp = random.Next(100000, 999999).ToString();
            return otp;
        }

        public async Task<string> SendOTPAsync(string email)
        {
            
            string otp = GenerateOTP();
            string subject = "Verify Your Email - EventSpot";
            string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Verification</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        body {{
            font-family: 'Inter', sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 0;
            background-color: #f4f4f5;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .header {{
            background-color: #0ea5e9;
            color: #ffffff;
            padding: 32px 24px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .logo-container {{
            text-align: center;
            margin: 20px 0;
        }}
        .logo-image {{
            max-width: 100%;
            height: auto;
            border-radius: 8px;
        }}
        .content {{
            padding: 32px 24px;
            background-color: #ffffff;
        }}
        .otp-container {{
            margin: 24px 0;
            text-align: center;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: 700;
            letter-spacing: 4px;
            color: #0ea5e9;
            background-color: #f0f9ff;
            padding: 16px 24px;
            border-radius: 8px;
            border: 2px dashed #0ea5e9;
            display: inline-block;
        }}
        .message {{
            color: #4b5563;
            margin-bottom: 24px;
        }}
        .warning {{
            font-size: 14px;
            color: #dc2626;
            background-color: #fef2f2;
            padding: 12px;
            border-radius: 6px;
            margin-top: 24px;
        }}
        .footer {{
            text-align: center;
            padding: 24px;
            background-color: #f8fafc;
            color: #6b7280;
            font-size: 14px;
            border-top: 1px solid #e5e7eb;
        }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background-color: #0ea5e9;
            color: #ffffff;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 500;
            margin-top: 16px;
        }}
        .timer {{
            font-size: 14px;
            color: #6b7280;
            margin-top: 16px;
        }}
        .divider {{
            height: 1px;
            background-color: #e5e7eb;
            margin: 24px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Email Verification</h1>
        </div>
        <div class='content'>
            <div class='logo-container'>
                <img src='https://utu.ac.in/index/Horizontal.jpg' alt='EventSpot Logo' class='logo-image'>
            </div>
            <div class='message'>
                <p>Hello,</p>
                <p>Thank you for registering with EventSpot. To complete your registration, please use the following verification code:</p>
            </div>
            
            <div class='otp-container'>
                <div class='otp-code'>{otp}</div>
                <p class='timer'>This code will expire in 2 minutes</p>
            </div>

            <div class='divider'></div>
            
            <div class='message'>
                <p><strong>Security Tips:</strong></p>
                <ul>
                    <li>Never share this code with anyone</li>
                    <li>Our team will never ask for this code</li>
                    <li>Make sure you're on the official EventSpot website</li>
                </ul>
            </div>

            <div class='warning'>
                ⚠️ If you didn't request this verification code, please ignore this email or contact support if you're concerned.
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>© 2025 EventSpot. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);

            _otpStorage[email] = (otp, DateTime.UtcNow.AddMinutes(2));

            return otp;
        }

        public bool VerifyOTP(string email, string otp)
        {
            
            if (_otpStorage.TryGetValue(email, out var storedData))
            {
                
                if (DateTime.UtcNow <= storedData.ExpiryTime && storedData.OTP == otp)
                {
                    _otpStorage.Remove(email);
                    return true;
                }
               
            }
           
            
            return false;
        }

        public bool VerifyForgotPasswordOTP(string email, string otp)
        {
            
            if (_forgotPasswordOtpStorage.TryGetValue(email, out var storedData))
            {
                
                if (DateTime.UtcNow <= storedData.ExpiryTime && storedData.OTP == otp)
                {
                    return true;
                }
            }
            return false;
        }
        public void RemoveForgotPasswordOTP(string email)
        {
            if (_forgotPasswordOtpStorage.ContainsKey(email))
            {
                _forgotPasswordOtpStorage.Remove(email);
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string username, string firstName)
        {
            var subject = "Welcome to EventSpot!";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to EventSpot</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        body {{
            font-family: 'Inter', sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 0;
            background-color: #f4f4f5;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .header {{
            background-color: #2563EB;
            color: #ffffff;
            padding: 32px 24px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .logo-container {{
            text-align: center;
            margin: 20px 0;
        }}
        .logo-image {{
            max-width: 200px;
            height: auto;
            border-radius: 8px;
        }}
        .content {{
            padding: 32px 24px;
            background-color: #ffffff;
        }}
        .welcome-message {{
            text-align: center;
            margin-bottom: 32px;
        }}
        .welcome-heading {{
            color: #2563EB;
            font-size: 28px;
            font-weight: 700;
            margin-bottom: 16px;
        }}
        .account-details {{
            background-color: #f0f9ff;
            border: 2px dashed #2563EB;
            border-radius: 8px;
            padding: 20px;
            margin: 24px 0;
            text-align: center;
        }}
        .username {{
            font-size: 20px;
            font-weight: 600;
            color: #2563EB;
            margin: 8px 0;
        }}
        .features-grid {{
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 16px;
            margin: 24px 0;
        }}
        .feature-item {{
            background-color: #f8fafc;
            padding: 16px;
            border-radius: 6px;
            text-align: center;
        }}
        .feature-icon {{
            font-size: 24px;
            color: #2563EB;
            margin-bottom: 8px;
        }}
        .cta-button {{
            display: inline-block;
            padding: 14px 32px;
            background-color: #2563EB;
            color: #ffffff;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            margin-top: 24px;
            transition: background-color 0.3s ease;
        }}
        .cta-button:hover {{
            background-color: #1d4ed8;
        }}
        .footer {{
            text-align: center;
            padding: 24px;
            background-color: #f8fafc;
            color: #6b7280;
            font-size: 14px;
            border-top: 1px solid #e5e7eb;
        }}
        .social-links {{
            margin-top: 16px;
        }}
        .social-links a {{
            color: #2563EB;
            text-decoration: none;
            margin: 0 8px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to EventSpot</h1>
        </div>
        <div class='content'>
            <div class='logo-container'>
                <img src='https://utu.ac.in/index/Horizontal.jpg' alt='EventSpot Logo' class='logo-image'>
            </div>
            
            <div class='welcome-message'>
                <h2 class='welcome-heading'>Welcome to EventSpot, {firstName}! 🎉</h2>
                <p>We're thrilled to have you join our community of event enthusiasts.</p>
            </div>

            <div class='account-details'>
                <p style='color: #4b5563; margin: 0;'>Your Account Details</p>
                <div class='username'>@{username}</div>
                <p style='color: #6b7280; font-size: 14px;'>Keep this information safe for future reference</p>
            </div>

            <div class='features-grid'>
                <div class='feature-item'>
                    <div class='feature-icon'>🎪</div>
                    <h3>Explore Venues</h3>
                    <p>Discover perfect venues for your events</p>
                </div>
                <div class='feature-item'>
                    <div class='feature-icon'>📅</div>
                    <h3>Book Venues</h3>
                    <p>Easy booking and management</p>
                </div>
                <div class='feature-item'>
                    <div class='feature-icon'>⭐</div>
                    <h3>Read Reviews</h3>
                    <p>Make informed decisions</p>
                </div>
                <div class='feature-item'>
                    <div class='feature-icon'>💼</div>
                    <h3>Manage Bookings</h3>
                    <p>Track all your events</p>
                </div>
            </div>

            <div style='text-align: center;'>
                <p style='margin-bottom: 24px;'>Ready to start planning your next event?</p>
                <a href='https://localhost:5184' class='cta-button'>Get Started Now</a>
            </div>
        </div>
        
        <div class='footer'>
            <p>Need help getting started? Our support team is here for you 24/7.</p>
            <div class='social-links'>
                <a href='#'>Facebook</a> |
                <a href='#'>Twitter</a> |
                <a href='#'>Instagram</a> |
                <a href='#'>LinkedIn</a>
            </div>
            <p style='margin-top: 16px;'>© 2025 EventSpot. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendForgotPasswordOTPAsync(string email)
        {
            if (_forgotPasswordOtpStorage.ContainsKey(email))
            {
                _forgotPasswordOtpStorage.Remove(email);
            }
            
            string otp = GenerateOTP();
            string subject = "Reset Your Password - EventSpot";
            string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        body {{
            font-family: 'Inter', sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 0;
            background-color: #f4f4f5;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .header {{
            background-color: #0ea5e9;
            color: #ffffff;
            padding: 32px 24px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .logo-container {{
            text-align: center;
            margin: 20px 0;
        }}
        .logo-image {{
            max-width: 100%;
            height: auto;
            border-radius: 8px;
        }}
        .content {{
            padding: 32px 24px;
            background-color: #ffffff;
        }}
        .otp-container {{
            margin: 24px 0;
            text-align: center;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: 700;
            letter-spacing: 4px;
            color: #0ea5e9;
            background-color: #f0f9ff;
            padding: 16px 24px;
            border-radius: 8px;
            border: 2px dashed #0ea5e9;
            display: inline-block;
        }}
        .message {{
            color: #4b5563;
            margin-bottom: 24px;
        }}
        .warning {{
            font-size: 14px;
            color: #dc2626;
            background-color: #fef2f2;
            padding: 12px;
            border-radius: 6px;
            margin-top: 24px;
        }}
        .footer {{
            text-align: center;
            padding: 24px;
            background-color: #f8fafc;
            color: #6b7280;
            font-size: 14px;
            border-top: 1px solid #e5e7eb;
        }}
        .timer {{
            font-size: 14px;
            color: #6b7280;
            margin-top: 16px;
        }}
        .divider {{
            height: 1px;
            background-color: #e5e7eb;
            margin: 24px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset</h1>
        </div>
        <div class='content'>
            <div class='logo-container'>
                <img src='https://utu.ac.in/index/Horizontal.jpg' alt='EventSpot Logo' class='logo-image'>
            </div>
            <div class='message'>
                <p>Hello,</p>
                <p>We received a request to reset your password. Please use the following verification code to proceed:</p>
            </div>
            
            <div class='otp-container'>
                <div class='otp-code'>{otp}</div>
                <p class='timer'>This code will expire in 5 minutes</p>
            </div>

            <div class='divider'></div>
            
            <div class='message'>
                <p><strong>Security Tips:</strong></p>
                <ul>
                    <li>Never share this code with anyone</li>
                    <li>Our team will never ask for this code</li>
                    <li>Make sure you're on the official EventSpot website</li>
                </ul>
            </div>

            <div class='warning'>
                ⚠️ If you didn't request this password reset, please ignore this email or contact support if you're concerned.
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>© 2025 EventSpot. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
            var expiryTime = DateTime.UtcNow.AddMinutes(5);
            _forgotPasswordOtpStorage[email] = (otp, expiryTime);
        }
    }
} 