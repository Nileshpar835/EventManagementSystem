using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Services;

namespace EventManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController( ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (User.IsInRole("Registrar"))
                {
                    return RedirectToAction("Index", "Registrar");
                }
                else if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("Index", "Customer");
                }
                return RedirectToAction("Index", "Home");
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.EmailOrUsername || u.Username == model.EmailOrUsername);
                
                if (user != null)
                {
                    
                    if (user.VerifyPassword(model.Password))
                    {
                        
                        user.LastLogin = DateTime.Now;
                        await _context.SaveChangesAsync();
                        
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Role, user.Role.ToString()),
                            new Claim(ClaimTypes.GivenName, user.FirstName),
                            new Claim(ClaimTypes.Surname, user.LastName),
                            new Claim("UserId", user.Id.ToString())
                        };
                        
                        
                        var claimsIdentity = new ClaimsIdentity(
                            claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                        };
                        
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme, 
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);
                        
                        
                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        else if (user.Role == UserRole.Admin)
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        else if (user.Role == UserRole.Registrar)
                        {
                            return RedirectToAction("Index", "Registrar");
                        }
                        else if (user.Role == UserRole.Customer)
                        {
                            return RedirectToAction("Index", "Customer");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (User.IsInRole("Registrar"))
                {
                    return RedirectToAction("Index", "Registrar");
                }
                else if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("Index", "Customer");
                }
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        return Json(new { success = false, message = "Email is already taken." });
                    }
                    
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    {
                        return Json(new { success = false, message = "Username is already taken." });
                    }

                    var user = new User
                    {
                        Email = model.Email,
                        Username = model.Username,
                        Password = Models.User.HashPassword(model.Password),
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                        Role = model.Role,
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    
                    if (model.Role == UserRole.Customer)
                    {
                        var customer = new Customer
                        {
                            UserId = user.Id,
                            CustomerId = "C" + user.Id.ToString("D5"),
                            Address = "",  
                            City = "",     
                            State = "",    
                            ZipCode = "",  
                            DateOfBirth = DateTime.Now, 
                            BookingsCount = 0,
                            FavoritesCount = 0,
                            ReviewsCount = 0,
                            PreferredEventTypesString = "" 
                        };
                        _context.Customers.Add(customer);
                    }
                    else if (model.Role == UserRole.Registrar)
                    {
                        var registrar = new Registrar
                        {
                            UserId = user.Id,
                            RegistrarId = "R" + user.Id.ToString("D5"),
                            OrganizationName = model.OrganizationName ?? "New Organization",
                            Position = model.Position ?? "Owner",
                            BusinessPhone = model.BusinessPhone ?? "Not Set",
                            BusinessAddress = model.BusinessAddress ?? "Not Set",
                            BusinessWebsite = model.BusinessWebsite ?? "",
                            BusinessEmail = model.BusinessEmail ?? user.Email,
                            TaxId = model.TaxId ?? "Not Set",
                            VenueSpecialties = model.VenueSpecialties ?? "Wedding",
                            IsApproved = true, 
                            VenuesCount = 0,
                            BookingsCount = 0,
                            ActiveBookingsCount = 0,
                            TotalRevenue = 0,
                            AverageRating = 0
                        };
                        _context.Registrars.Add(registrar);
                    }
                    else if (model.Role == UserRole.Admin)
                    {
                        var admin = new Admin
                        {
                            UserId = user.Id,
                            AdminId = "A" + user.Id.ToString("D5"),
                            Department = "System Administration",
                            Position = "Administrator",
                            AccessLevel = 5
                        };
                        _context.Admins.Add(admin);
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role.ToString()),
                        new Claim(ClaimTypes.GivenName, user.FirstName),
                        new Claim(ClaimTypes.Surname, user.LastName),
                        new Claim("UserId", user.Id.ToString())
                    };
                    
                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    };
                    
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Email, user.Username, user.FirstName);
                    }
                    catch (Exception emailEx)
                    {
                    }

                    string redirectUrl = model.Role switch
                    {
                        UserRole.Admin => "/Admin",
                        UserRole.Registrar => "/Registrar",
                        UserRole.Customer => "/Customer",
                        _ => "/"
                    };

                    return Json(new { success = true, redirectUrl });
                }
                
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = string.Join(", ", errors) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred during registration. Please try again." });
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult AccessDenied()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("Index", "Customer");
                }
                else if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (User.IsInRole("Registrar"))
                {
                    return RedirectToAction("Index", "Registrar");
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendOTP(string email)
        {
            try
            {

                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Email is required" });
                }

                if (!IsValidEmail(email))
                {
                    return Json(new { success = false, message = "Invalid email format" });
                }

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "This email is already registered. Please use a different email or login with your existing account." });
                }

                await _emailService.SendOTPAsync(email);
                
                return Json(new { success = true, message = "OTP sent successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to send OTP. Please try again later." });
            }
        }

        [HttpPost]
        public IActionResult VerifyOTP(string email, string otp)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
                {
                    return Json(new { success = false, message = "Email and OTP are required" });
                }

                if (!IsValidEmail(email))
                {
                    return Json(new { success = false, message = "Invalid email format" });
                }

                bool isValid = _emailService.VerifyOTP(email, otp);
                return Json(new { success = isValid, message = isValid ? "OTP verified successfully" : "Invalid OTP" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to verify OTP. Please try again later." });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendForgotPasswordOTP(string email)
        {
            try
            {

                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Email is required" });
                }

                if (!IsValidEmail(email))
                {
                    return Json(new { success = false, message = "Invalid email format" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return Json(new { success = false, message = "No account found with this email address." });
                }

                await _emailService.SendForgotPasswordOTPAsync(email);
                
                return Json(new { success = true, message = "OTP sent successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to send OTP. Please try again later." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string otp, string newPassword, string confirmPassword)
        {
            try
            {

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp) || 
                    string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                {
                    return Json(new { success = false, message = "All fields are required" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Passwords do not match" });
                }

                if (!_emailService.VerifyForgotPasswordOTP(email, otp))
                {
                    return Json(new { success = false, message = "Invalid or expired OTP" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.Password = Models.User.HashPassword(newPassword);
                await _context.SaveChangesAsync();

                _emailService.RemoveForgotPasswordOTP(email);

                return Json(new { success = true, message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to reset password. Please try again later." });
            }
        }

        [HttpPost]
        public IActionResult VerifyForgotPasswordOTP(string email, string otp)
        {
            try
            {

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
                {
                    return Json(new { success = false, message = "Email and OTP are required" });
                }

                if (!IsValidEmail(email))
                {
                    return Json(new { success = false, message = "Invalid email format" });
                }

                bool isValid = _emailService.VerifyForgotPasswordOTP(email, otp);
                if (!isValid)
                {
                    return Json(new { success = false, message = "Invalid or expired OTP" });
                }

                return Json(new { success = true, message = "OTP verified successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to verify OTP. Please try again later." });
            }
        }
    }
} 