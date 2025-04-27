using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using EventManagementSystem.Models.ViewModels;
using EventManagementSystem.Services;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EventManagementSystem.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly VenueService _venueService;
        private readonly BookingDbService _bookingService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserService _userService;
        private readonly ApplicationDbContext _context;

        public CustomerController(
            VenueService venueService,
            BookingDbService bookingService,
            IWebHostEnvironment webHostEnvironment,
            UserService userService,
            ApplicationDbContext context)
        {
            _venueService = venueService;
            _bookingService = bookingService;
            _webHostEnvironment = webHostEnvironment;
            _userService = userService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            
            
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }
            try 
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);
                
                if (user != null)
                {
                }
                else
                {
                    return RedirectToAction("Login", "Account");
                }
                int userIdInt = user.Id;
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt);

                
                if (customer != null)
                {
                    var allBookings = _bookingService.GetBookingsByCustomerId(userIdInt);
                    var upcomingBookings = allBookings
                        .Where(b => b.Date >= DateTime.Now.Date && b.Status != "Cancelled")
                        .OrderBy(b => b.Date)
                        .ThenBy(b => b.StartTime)
                        .Take(5) 
                        .ToList();

                    foreach (var booking in upcomingBookings)
                    {
                        if (booking.Venue == null && booking.VenueId > 0)
                        {
                            booking.Venue = _venueService.GetVenueById(booking.VenueId);
                            if (booking.Venue != null)
                            {
                            }
                            else
                            {
                            }
                        }
                    }
                    customer.UpcomingBookings = upcomingBookings;
                    customer.BookingsCount = allBookings.Count(b => b.Status == "Confirmed" || b.Status == "Completed");
                    customer.ReviewsCount = _context.VenueReviews.Count(r => r.UserId == userIdInt);
                    customer.User = user;
                    customer.RecommendedVenues = _venueService.GetRecommendedVenuesForCustomer(customer);
                }
                else
                {
                    customer = new Customer
                    {
                        UserId = userIdInt,
                        User = user,
                        CustomerId = $"C{userIdInt:D5}",
                        Address = "",
                        City = "",
                        State = "",
                        ZipCode = "",
                        DateOfBirth = DateTime.MinValue,
                        BookingsCount = 0,
                        FavoritesCount = 0,
                        ReviewsCount = 0,
                        PreferredEventTypesString = ""
                    };
                    
                    try
                    {
                        _context.Entry(user).State = EntityState.Unchanged;
                        _context.Customers.Add(customer);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                    }
                }

                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserLastName"] = user.LastName;
                ViewData["UserEmail"] = user.Email;
                ViewData["UserProfileImage"] = user.ProfilePicture;
                
                ViewData["TotalBookings"] = customer.BookingsCount;
                ViewData["FavoritesCount"] = customer.FavoritesCount;
                ViewData["ReviewsCount"] = customer.ReviewsCount;
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_IndexPartial", customer);
                }
                
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading your dashboard.";
                return View(null);
            }
        }
        
        public IActionResult Bookings()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var user = _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserLastName"] = user.LastName;
                ViewData["UserEmail"] = user.Email;
                ViewData["UserProfileImage"] = user.ProfilePicture;
                ViewData["CreatedAt"] = user.CreatedAt;
                ViewData["ProfilePictureUrl"] = !string.IsNullOrEmpty(user.ProfilePicture) 
                    ? user.ProfilePicture 
                    : $"https://ui-avatars.com/api/?name={user.FirstName}+{user.LastName}&background=0284c7&color=fff";
                
                int customerId = user.Id;
                var bookings = _bookingService.GetBookingsByCustomerId(customerId);
                foreach (var booking in bookings)
                {
                    if (booking.Venue == null && booking.VenueId > 0)
                    {
                        booking.Venue = _venueService.GetVenueById(booking.VenueId);
                    }
                }
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_BookingsPartial", bookings);
                }
                
                return View(bookings);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Error");
            }
        }
        
        [HttpPost]
        public IActionResult CancelBooking(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }
            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Username == username);
            
            if (user == null || booking.CustomerId != user.Id)
            {
                return Unauthorized();
            }
            
            booking.Status = "Cancelled";
            _bookingService.UpdateBooking(booking);
            
            TempData["SuccessMessage"] = "Your booking has been cancelled successfully.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                int customerId = user.Id;
                var bookings = _bookingService.GetBookingsByCustomerId(customerId);
                foreach (var b in bookings)
                {
                    if (b.Venue == null && b.VenueId > 0)
                    {
                        b.Venue = _venueService.GetVenueById(b.VenueId);
                    }
                }
                
                return PartialView("_BookingsPartial", bookings);
            }
            
            return RedirectToAction(nameof(Bookings));
        }
        
        public IActionResult BrowseVenues()
        {
            var venues = _venueService.GetAllVenues();
            return View(venues);
        }
        
        public async Task<IActionResult> Profile()
        {
            
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            try 
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);
                
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                int userIdInt = user.Id;
                
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        UserId = userIdInt,
                        CustomerId = $"C{userIdInt:D5}",
                        Address = "",
                        City = "",
                        State = "",
                        ZipCode = "",
                        DateOfBirth = DateTime.MinValue
                    };
                    
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                var viewModel = new ProfileUpdateViewModel
                {
                    Id = customer.Id,
                    UserId = customer.UserId,
                    CustomerId = customer.CustomerId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Address = customer.Address,
                    City = customer.City,
                    State = customer.State,
                    ZipCode = customer.ZipCode,
                    DateOfBirth = customer.DateOfBirth,
                    PreferredEventTypesString = customer.PreferredEventTypesString
                };

                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserLastName"] = user.LastName;
                ViewData["UserEmail"] = user.Email;
                ViewData["UserProfileImage"] = user.ProfilePicture;
                ViewData["CreatedAt"] = user.CreatedAt;
                ViewData["ProfilePictureUrl"] = !string.IsNullOrEmpty(user.ProfilePicture) 
                    ? user.ProfilePicture 
                    : $"https://ui-avatars.com/api/?name={user.FirstName}+{user.LastName}&background=0284c7&color=fff";
            
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_ProfilePartial", viewModel);
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Index", "Customer");
            }
        }
        
        public IActionResult Notifications()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_NotificationsPartial");
            }
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    TempData["ErrorMessage"] = string.Join(". ", errors);
                    return RedirectToAction("Profile");
                }
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Account");
                }
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
                
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                int userIdInt = user.Id;
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt);
                bool isNewCustomer = false;
                
                if (customer == null)
                {
                    isNewCustomer = true;
                    customer = new Customer
                    {
                        UserId = userIdInt,
                        CustomerId = $"C{userIdInt:D5}"
                    };
                }
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.PhoneNumber = model.Phone;
                customer.Address = string.IsNullOrEmpty(model.Address) ? "" : model.Address.Trim();
                customer.City = string.IsNullOrEmpty(model.City) ? "" : model.City.Trim();
                customer.State = string.IsNullOrEmpty(model.State) ? "" : model.State.Trim();
                customer.ZipCode = string.IsNullOrEmpty(model.ZipCode) ? "" : model.ZipCode.Trim();
                if (model.DateOfBirth != DateTime.MinValue && model.DateOfBirth.Year > 1900)
                {
                    customer.DateOfBirth = model.DateOfBirth;
                }
                
                if (!string.IsNullOrEmpty(model.PreferredEventTypesString))
                {
                    customer.PreferredEventTypesString = model.PreferredEventTypesString;
                }
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    try
                    {
                        
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(model.ProfilePicture.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ProfilePicture.CopyToAsync(stream);
                        }
                        user.ProfilePicture = $"/images/profiles/{uniqueFileName}";
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = "Failed to upload profile picture. Please try again.";
                    }
                }

                _context.Entry(user).State = EntityState.Modified;
                
                if (isNewCustomer)
                {
                    _context.Customers.Add(customer);
                }
                else
                {
                    _context.Entry(customer).State = EntityState.Modified;
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Profile updated successfully!";

                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserLastName"] = user.LastName;
                ViewData["UserEmail"] = user.Email;
                ViewData["UserProfileImage"] = user.ProfilePicture;

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again.";
                return RedirectToAction("Profile");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction("Profile");
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match.";
                return RedirectToAction("Profile");
            }

            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
                
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var hashedCurrentPassword = HashPassword(CurrentPassword);
                if (user.Password != hashedCurrentPassword)
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction("Profile");
                }
                user.Password = HashPassword(NewPassword);
                
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Password changed successfully!";

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while changing your password.";
                return RedirectToAction("Profile");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> UpdatePreferences(Customer model, List<string> PreferredEventTypes)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
                
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                int userIdInt = user.Id;
                
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt);
                    
                if (customer == null)
                {
                    customer = new Customer
                    {
                        UserId = userIdInt,
                        CustomerId = $"C{userIdInt:D5}",
                        Address = "",
                        City = "",
                        State = "",
                        ZipCode = "",
                        DateOfBirth = DateTime.MinValue,
                        BookingsCount = 0,
                        FavoritesCount = 0,
                        ReviewsCount = 0
                    };
                    
                    _context.Customers.Add(customer);
                }

                string preferredEventTypesString = PreferredEventTypes != null && PreferredEventTypes.Any() 
                    ? string.Join(", ", PreferredEventTypes) 
                    : "";
                customer.PreferredEventTypesString = preferredEventTypesString;
                
                _context.Entry(user).State = EntityState.Unchanged;
                
                if (_context.Entry(customer).State == EntityState.Detached)
                {
                    _context.Attach(customer);
                    _context.Entry(customer).Property(c => c.PreferredEventTypesString).IsModified = true;
                }
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Preferences updated successfully!";

                ViewData["UserFirstName"] = user.FirstName;
                ViewData["UserLastName"] = user.LastName;
                ViewData["UserEmail"] = user.Email;
                ViewData["UserProfileImage"] = user.ProfilePicture;

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_ProfilePartial", customer);
                }

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating your preferences.";
                return RedirectToAction("Profile");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
                
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                int userIdInt = user.Id;

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt);
                if (customer != null)
                {
                }
                
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                TempData["SuccessMessage"] = "Your account has been deleted successfully.";

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting your account.";
                return RedirectToAction("Profile");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(int Id, int UserId, string CustomerId, IFormFile ProfilePicture)
        {
            try
            {
                
                if (ProfilePicture == null || ProfilePicture.Length == 0)
                {
                    return Json(new { success = false, message = "No file was uploaded." });
                }

                if (!ProfilePicture.ContentType.StartsWith("image/"))
                {
                    return Json(new { success = false, message = "Please upload an image file." });
                }

                if (ProfilePicture.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Image size should be less than 5MB." });
                }

                var user = await _context.Users.FindAsync(UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfilePicture.FileName)}";
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePicture.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                user.ProfilePicture = $"/images/profiles/{fileName}";
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile picture updated successfully!", imageUrl = user.ProfilePicture });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating your profile picture." });
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
} 
