using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using EventManagementSystem.Services;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace EventManagementSystem.Controllers
{
    [Authorize(Roles = "Registrar")]
    public class RegistrarController : Controller
    {
        private readonly VenueService _venueService;
        private readonly BookingDbService _bookingService;
        private readonly ApplicationDbContext _context;

        public RegistrarController(VenueService venueService, BookingDbService bookingService, ApplicationDbContext context)
        {
            _venueService = venueService;
            _bookingService = bookingService;
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (userId != 0)
            {
                var registrar = _context.Registrars
                    .Include(r => r.User)
                    .FirstOrDefault(r => r.UserId == userId);

                if (registrar != null)
                {
                    ViewData["Registrar"] = registrar;
                }
            }
        }

        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null || user.Role != UserRole.Registrar)
            {
                return RedirectToAction("Login", "Account");
            }

            var registrar = _context.Registrars
                .Include(r => r.User)
                .FirstOrDefault(r => r.UserId == userId);

            if (registrar == null)
            {
                registrar = new Registrar
                {
                    UserId = userId,
                    RegistrarId = "R" + userId.ToString("D5"),
                    User = user,
                    IsApproved = true,
                    ApprovalDate = DateTime.Now,
                    VenuesCount = 0,
                    BookingsCount = 0,
                    ActiveBookingsCount = 0,
                    TotalRevenue = 0,
                    AverageRating = 0,
                    VenueSpecialties = "Wedding", 
                    OrganizationName = "New Organization", 
                    Position = "Owner",
                    BusinessPhone = "Not Set",
                    BusinessAddress = "Not Set",
                    BusinessWebsite = "",
                    BusinessEmail = user.Email, 
                    TaxId = "Not Set"
                };

                _context.Registrars.Add(registrar);
                _context.SaveChanges();
            }

            registrar.Venues = _venueService.GetVenuesByRegistrarId(registrar.Id);

            registrar.VenuesCount = registrar.Venues.Count;
            _context.SaveChanges();

            registrar.RecentBookings = _bookingService.GetBookingsByRegistrarId(registrar.Id)
                .Take(5)
                .ToList();

            registrar.ActiveBookingsCount = _context.Bookings
                .Include(b => b.Venue)
                .Where(b => b.Venue.RegistrarId == registrar.Id && b.Status == "Confirmed")
                .Count();

            var registrarVenues = _context.Venues
                .Where(v => v.RegistrarId == registrar.Id)
                .Select(v => v.Id)
                .ToList();

            var reviews = _context.VenueReviews
                .Where(r => registrarVenues.Contains(r.VenueId));

            registrar.AverageRating = reviews.Any()
                ? Math.Round(reviews.Average(r => r.Rating), 1)
                : 0;

            registrar.TotalRevenue = _context.Bookings
                .Include(b => b.Venue)
                .Where(b => b.Venue.RegistrarId == registrar.Id &&
                           (b.Status == "Confirmed" || b.Status == "Completed"))
                .Sum(b => b.TotalPrice);

            return View(registrar);
        }

        public IActionResult MyVenues(bool showEmpty = false)
        {
            if (showEmpty)
            {
                _venueService.SetEmptyListMode(true);
            }

            var registrar = ViewData["Registrar"] as Registrar;
            if (registrar == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var venuesList = _venueService.GetVenuesByRegistrarId(registrar.Id);

                if (showEmpty)
                {
                    _venueService.SetEmptyListMode(false);
                }

                return View(venuesList);
            }

            var venues = _venueService.GetVenuesByRegistrarId(registrar.Id);

            if (showEmpty)
            {
                _venueService.SetEmptyListMode(false);
            }

            return View(venues);
        }

        public IActionResult Venues()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var venueData = _venueService.GetAllVenues();
                return View("MyVenues", venueData);
            }

            return RedirectToAction(nameof(MyVenues));
        }

        public IActionResult VenueDetails(int id)
        {
            var venue = _venueService.GetVenueById(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }

        [HttpGet]
        public IActionResult AddVenue()
        {
            var venue = new Venue
            {
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Features = "",
                VenueTypes = "",
                AvailableEventTypes = "",
                AdditionalImages = "",
                Rating = 0,
                ReviewCount = 0,
                HostBookingCount = 0,
                HostResponseRate = 100,
                HostResponseTime = 24,
                HostMemberSince = DateTime.Now,
                ImageUrl = "/images/venues/default.jpg",
                HostImage = "/images/hosts/default.jpg",
                HostName = "Host"
            };
            return View(venue);
        }

        [HttpPost]
        public IActionResult AddVenue(Venue venue, IFormFile? mainImage, IFormFile[]? additionalImages, IFormFile? hostImage)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var registrar = _context.Registrars
                    .Include(r => r.User)
                    .FirstOrDefault(r => r.UserId == userId);

                if (registrar == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var newVenue = new Venue
                {
                    Name = venue.Name,
                    Location = venue.Location,
                    Address = venue.Address,
                    Capacity = venue.Capacity,
                    PricePerHour = venue.PricePerHour,

                    Description = venue.Description ?? "",
                    Type = venue.Type ?? "Venue",
                    Features = venue.Features ?? "",
                    VenueTypes = venue.VenueTypes ?? "",
                    AvailableEventTypes = venue.AvailableEventTypes ?? venue.VenueTypes ?? "",
                    AdditionalImages = "",
                    Amenities = venue.Amenities ?? "",
                    MaxCapacity = venue.MaxCapacity,
                    Size = venue.Size,
                    CleaningFee = venue.CleaningFee,
                    ContactEmail = venue.ContactEmail,
                    ContactPhone = venue.ContactPhone,

                    RegistrarId = registrar.Id,

                    HostName = venue.HostName ,
                    HostMemberSince = DateTime.Now,
                    HostBookingCount = 0,
                    HostResponseRate = 100,
                    HostResponseTime = 24,

                    IsActive = true,
                    IsAvailable = true,
                    Status = "Active",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Rating = 0,
                    ReviewCount = 0
                };

                if (string.IsNullOrEmpty(newVenue.Name))
                {
                    ModelState.AddModelError("Name", "Venue name is required");
                }

                if (string.IsNullOrEmpty(newVenue.Location))
                {
                    ModelState.AddModelError("Location", "Venue location is required");
                }

                if (string.IsNullOrEmpty(newVenue.Address))
                {
                    ModelState.AddModelError("Address", "Venue address is required");
                }

                if (newVenue.Capacity <= 0)
                {
                    ModelState.AddModelError("Capacity", "Venue capacity must be greater than 0");
                }

                if (newVenue.PricePerHour <= 0)
                {
                    ModelState.AddModelError("PricePerHour", "Price per hour must be greater than 0");
                }

                if (!ModelState.IsValid)
                {
                    return View(venue);
                }

                if (mainImage != null && mainImage.Length > 0)
                {
                    var fileName = Path.GetFileName(mainImage.FileName);
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = $"venue_{DateTime.Now.Ticks}{fileExtension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "venues", newFileName);

                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        mainImage.CopyTo(stream);
                    }

                    newVenue.ImageUrl = $"/images/venues/{newFileName}";
                }
                else
                {
                    newVenue.ImageUrl = "/images/venues/default.jpg";
                }

                if (hostImage != null && hostImage.Length > 0)
                {
                    var fileName = Path.GetFileName(hostImage.FileName);
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = $"host_{DateTime.Now.Ticks}{fileExtension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "hosts", newFileName);

                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        hostImage.CopyTo(stream);
                    }

                    newVenue.HostImage = $"/images/hosts/{newFileName}";
                }
                else
                {
                    newVenue.HostImage = "/images/hosts/default.jpg";
                }

                if (additionalImages != null && additionalImages.Length > 0)
                {
                    var newImages = new List<string>();

                    foreach (var image in additionalImages)
                    {
                        if (image.Length > 0)
                        {
                            var fileName = Path.GetFileName(image.FileName);
                            var fileExtension = Path.GetExtension(fileName);
                            var newFileName = $"venue_additional_{DateTime.Now.Ticks}{fileExtension}";
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "venues", newFileName);

                            var directory = Path.GetDirectoryName(filePath);
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                image.CopyTo(stream);
                            }

                            newImages.Add($"/images/venues/{newFileName}");
                        }
                    }

                    newVenue.AdditionalImages = string.Join(",", newImages);
                }

                _context.Venues.Add(newVenue);
                _context.SaveChanges();

                registrar.VenuesCount = _context.Venues.Count(v => v.RegistrarId == registrar.Id);
                _context.SaveChanges();

                return RedirectToAction(nameof(MyVenues));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the venue. Please try again.");
                return View(venue);
            }
        }

        [HttpGet]
        public IActionResult EditVenue(int id)
        {
            var venue = _venueService.GetVenueById(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }

        [HttpPost]
        public IActionResult EditVenue(Venue venue, IFormFile? mainImage, IFormFile[]? additionalImages, string[]? existingAdditionalImages)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(venue);
                }

                var existingVenue = _context.Venues.FirstOrDefault(v => v.Id == venue.Id);
                if (existingVenue == null)
                {
                    return NotFound();
                }

                existingVenue.Name = venue.Name;
                existingVenue.Description = venue.Description;
                existingVenue.Type = venue.Type;
                existingVenue.Location = venue.Location;
                existingVenue.Address = venue.Address;
                existingVenue.Capacity = venue.Capacity;
                existingVenue.Size = venue.Size;
                existingVenue.PricePerHour = venue.PricePerHour;
                existingVenue.CleaningFee = venue.CleaningFee;
                existingVenue.IsActive = venue.IsActive;
                existingVenue.IsAvailable = venue.IsAvailable;
                existingVenue.Status = venue.Status;
                existingVenue.IsFeatured = venue.IsFeatured;

                existingVenue.VenueTypes = venue.VenueTypes;
                existingVenue.AvailableEventTypes = venue.AvailableEventTypes;
                existingVenue.Amenities = venue.Amenities;
                existingVenue.Features = venue.Features;

                if (mainImage != null && mainImage.Length > 0)
                {
                    var fileName = Path.GetFileName(mainImage.FileName);
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = $"venue_{venue.Id}_{DateTime.Now.Ticks}{fileExtension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "venues", newFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        mainImage.CopyTo(stream);
                    }

                    existingVenue.ImageUrl = $"/images/venues/{newFileName}";
                }

                existingVenue.AdditionalImages = venue.AdditionalImages ?? "";

                if (existingAdditionalImages != null && existingAdditionalImages.Length > 0)
                {
                    existingVenue.AdditionalImages = string.Join(",", existingAdditionalImages);
                }

                if (additionalImages != null && additionalImages.Length > 0)
                {
                    var newImages = new List<string>();

                    if (!string.IsNullOrEmpty(existingVenue.AdditionalImages))
                    {
                        newImages.AddRange(existingVenue.AdditionalImages.Split(','));
                    }

                    foreach (var image in additionalImages)
                    {
                        if (image.Length > 0)
                        {
                            var fileName = Path.GetFileName(image.FileName);
                            var fileExtension = Path.GetExtension(fileName);
                            var newFileName = $"venue_{venue.Id}_additional_{DateTime.Now.Ticks}{fileExtension}";
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "venues", newFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                image.CopyTo(stream);
                            }

                            newImages.Add($"/images/venues/{newFileName}");
                        }
                    }

                    existingVenue.AdditionalImages = string.Join(",", newImages);
                }

                existingVenue.UpdatedAt = DateTime.Now;

                _context.Venues.Update(existingVenue);
                var result = _context.SaveChanges();

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Venue updated successfully!";
                    return RedirectToAction(nameof(MyVenues));
                }
                else
                {
                    ModelState.AddModelError("", "No changes were made to the venue.");
                    return View(venue);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the venue. Please try again.");
                return View(venue);
            }
        }

        [HttpGet]
        public IActionResult DeleteVenue(int id)
        {
            var venue = _venueService.GetVenueById(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }

        [HttpPost, ActionName("DeleteVenue")]
        public IActionResult DeleteVenueConfirmed(int id)
        {
            _venueService.DeleteVenue(id);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Bookings()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return View(GetRegistrarBookings());
            }

            return View(GetRegistrarBookings());
        }

        public IActionResult Profile()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return View(GetRegistrarProfile());
            }

            return View(GetRegistrarProfile());
        }

        [HttpPost]
        public IActionResult UpdateProfile(Registrar registrar, IFormFile ProfileImage)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                var currentRegistrar = _context.Registrars
                    .Include(r => r.User)
                    .FirstOrDefault(r => r.UserId == userId);

                if (currentRegistrar == null)
                {
                    TempData["ErrorMessage"] = "Registrar not found.";
                    return RedirectToAction(nameof(Profile));
                }

                currentRegistrar.BusinessPhone = registrar.BusinessPhone ?? currentRegistrar.BusinessPhone;
                currentRegistrar.BusinessEmail = registrar.BusinessEmail ?? currentRegistrar.BusinessEmail;
                currentRegistrar.BusinessAddress = registrar.BusinessAddress ?? currentRegistrar.BusinessAddress;
                currentRegistrar.Position = registrar.Position ?? currentRegistrar.Position;
                currentRegistrar.BusinessWebsite = registrar.BusinessWebsite ?? currentRegistrar.BusinessWebsite;
                currentRegistrar.VenueSpecialties = registrar.VenueSpecialties ?? currentRegistrar.VenueSpecialties;
                currentRegistrar.OrganizationName = registrar.OrganizationName ?? currentRegistrar.OrganizationName;

                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "registrars");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"registrar_{currentRegistrar.Id}_{DateTime.Now.Ticks}{Path.GetExtension(ProfileImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        ProfileImage.CopyTo(fileStream);
                    }

                    currentRegistrar.User.ProfilePicture = $"/images/registrars/{uniqueFileName}";
                }

                if (currentRegistrar.User != null)
                {
                    currentRegistrar.User.FirstName = registrar.FirstName ?? currentRegistrar.User.FirstName;
                    currentRegistrar.User.LastName = registrar.LastName ?? currentRegistrar.User.LastName;
                    currentRegistrar.User.Email = registrar.Email ?? currentRegistrar.User.Email;
                    currentRegistrar.User.PhoneNumber = registrar.PhoneNumber ?? currentRegistrar.User.PhoneNumber;
                }

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your profile has been successfully updated!";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again.";
                return View("Profile", registrar);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int Id, string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction(nameof(Profile));
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Profile));
                }

                var hashedCurrentPassword = Models.User.HashPassword(CurrentPassword);
                if (user.Password != hashedCurrentPassword)
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction(nameof(Profile));
                }

                user.Password = Models.User.HashPassword(NewPassword);

                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your password has been successfully changed!";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while changing your password. Please try again.";
                return RedirectToAction(nameof(Profile));
            }
        }

        

        public IActionResult Analytics()
        {
            return View();
        }

        public IActionResult Reports()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return View(GetRegistrarReports());
            }

            return View(GetRegistrarReports());
        }

        public IActionResult ConfirmBooking(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Confirmed";
            _bookingService.UpdateBooking(booking);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Booking has been confirmed successfully!" });
            }

            TempData["SuccessMessage"] = "Booking has been confirmed successfully!";
            return RedirectToAction(nameof(Bookings));
        }

        public IActionResult CancelBooking(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Cancelled";
            _bookingService.UpdateBooking(booking);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Booking has been cancelled successfully!" });
            }

            TempData["SuccessMessage"] = "Booking has been cancelled successfully!";
            return RedirectToAction(nameof(Bookings));
        }

        public IActionResult CompleteBooking(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Completed";
            _bookingService.UpdateBooking(booking);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Booking has been marked as completed!" });
            }

            TempData["SuccessMessage"] = "Booking has been marked as completed!";
            return RedirectToAction(nameof(Bookings));
        }

        public IActionResult BookingDetails(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Venue = _venueService.GetVenueById(booking.VenueId);
            return PartialView("_BookingDetailsPartial", booking);
        }

        public IActionResult Reviews()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return View(GetRegistrarReviews());
            }

            return View(GetRegistrarReviews());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private Registrar GetRegistrarDashboardData()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (userId == 0)
            {
                return new Registrar();
            }

            var registrar = _context.Registrars
                .Include(r => r.User)
                .FirstOrDefault(r => r.UserId == userId);

            if (registrar == null)
            {
                return new Registrar();
            }

            registrar.Venues = _venueService.GetVenuesByRegistrarId(registrar.Id);

            registrar.VenuesCount = registrar.Venues.Count;
            _context.SaveChanges();

            registrar.RecentBookings = _bookingService.GetBookingsByRegistrarId(registrar.Id)
                .Take(5)
                .ToList();

            registrar.VenuesCount = registrar.Venues?.Count ?? 0;
            registrar.ActiveBookingsCount = registrar.RecentBookings?.Count(b => b.Status == "Confirmed") ?? 0;
            registrar.TotalRevenue = registrar.RecentBookings?.Sum(b => b.TotalPrice) ?? 0;
            registrar.AverageRating = registrar.Venues?.Average(v => v.Rating) ?? 0;

            var categories = registrar.Venues?
                .SelectMany(v => v.VenueTypes?.Split(',') ?? new string[0])
                .Distinct()
                .ToList() ?? new List<string>();
            registrar.VenueCategories = categories;

            return registrar;
        }

        private List<Booking> GetRegistrarBookings()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (userId == 0)
            {
                return new List<Booking>();
            }

            var registrar = _context.Registrars
                .FirstOrDefault(r => r.UserId == userId);

            if (registrar == null)
            {
                return new List<Booking>();
            }

            var bookings = _context.Bookings
                .Include(b => b.Venue)
                .Where(b => b.Venue.RegistrarId == registrar.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            foreach (var booking in bookings)
            {
                var customer = _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefault(c => c.Id == booking.CustomerId);

                if (customer != null && customer.User != null)
                {
                    booking.CustomerName = $"{customer.User.FirstName} {customer.User.LastName}";
                    booking.CustomerEmail = customer.User.Email;
                    booking.CustomerPhone = customer.User.PhoneNumber;
                    booking.CustomerProfilePicture = !string.IsNullOrEmpty(customer.User.ProfilePicture)
                        ? customer.User.ProfilePicture
                        : $"https://ui-avatars.com/api/?name={customer.User.FirstName}+{customer.User.LastName}&background=0284c7&color=fff";
                }
            }

            return bookings;
        }

        private List<Venue> GetRegistrarVenues()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (userId == 0)
            {
                return new List<Venue>();
            }

            var registrar = _context.Registrars
                .FirstOrDefault(r => r.UserId == userId);

            if (registrar == null)
            {
                return new List<Venue>();
            }

            return _context.Venues
                .Where(v => v.RegistrarId == registrar.Id)
                .OrderBy(v => v.Name)
                .ToList();
        }

        private object GetRegistrarReports()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (userId == 0)
            {
                return new
                {
                    MonthlyRevenue = 0,
                    YearlyRevenue = 0,
                    TopVenue = "N/A",
                    MostBookedCategory = "N/A"
                };
            }

            var registrar = _context.Registrars
                .FirstOrDefault(r => r.UserId == userId);

            if (registrar == null)
            {
                return new
                {
                    MonthlyRevenue = 0,
                    YearlyRevenue = 0,
                    TopVenue = "N/A",
                    MostBookedCategory = "N/A"
                };
            }

            var monthlyRevenue = _context.Bookings
                .Include(b => b.Venue)
                .Where(b => b.Venue.RegistrarId == registrar.Id &&
                           b.CreatedAt >= DateTime.Now.AddDays(-30) &&
                           (b.Status == "Confirmed" || b.Status == "Completed"))
                .Sum(b => b.TotalPrice);

            var yearlyRevenue = _context.Bookings
                .Include(b => b.Venue)
                .Where(b => b.Venue.RegistrarId == registrar.Id &&
                           b.CreatedAt >= DateTime.Now.AddMonths(-12) &&
                           (b.Status == "Confirmed" || b.Status == "Completed"))
                .Sum(b => b.TotalPrice);

            var topVenue = _context.Bookings
                .Include(b => b.Venue)
                .Where(b => b.Venue.RegistrarId == registrar.Id &&
                           (b.Status == "Confirmed" || b.Status == "Completed"))
                .GroupBy(b => b.Venue.Name)
                .Select(g => new { VenueName = g.Key, Revenue = g.Sum(b => b.TotalPrice) })
                .OrderByDescending(x => x.Revenue)
                .FirstOrDefault();

            var mostBookedCategory = _context.Bookings
                .Include(b => b.Venue)
                .Where(b => b.Venue.RegistrarId == registrar.Id &&
                           (b.Status == "Confirmed" || b.Status == "Completed"))
                .GroupBy(b => b.Venue.VenueTypes)
                .Select(g => new { VenueType = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            return new
            {
                MonthlyRevenue = monthlyRevenue,
                YearlyRevenue = yearlyRevenue,
                TopVenue = topVenue?.VenueName ?? "N/A",
                MostBookedCategory = mostBookedCategory?.VenueType ?? "N/A"
            };
        }

        private Registrar GetRegistrarProfile()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (userId == 0)
            {
                return new Registrar();
            }

            var registrar = _context.Registrars
                .Include(r => r.User)
                .FirstOrDefault(r => r.UserId == userId);

            if (registrar == null)
            {
                return new Registrar();
            }

            registrar.Venues = _venueService.GetVenuesByRegistrarId(registrar.Id);

            registrar.RecentBookings = _bookingService.GetBookingsByRegistrarId(registrar.Id)
                .Take(5)
                .ToList();

            return registrar;
        }

        private object GetRegistrarReviews()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (userId == 0)
            {
                return new { AverageRating = 0.0, TotalReviews = 0, PositiveReviews = 0, NegativeReviews = 0, Reviews = new List<object>() };
            }

            var registrar = _context.Registrars.FirstOrDefault(r => r.UserId == userId);
            if (registrar == null)
            {
                return new { AverageRating = 0.0, TotalReviews = 0, PositiveReviews = 0, NegativeReviews = 0, Reviews = new List<object>() };
            }

            var registrarVenues = _context.Venues.Where(v => v.RegistrarId == registrar.Id).Select(v => v.Id).ToList();

            var reviews = _context.VenueReviews
                .Include(r => r.Venue)
                .Where(r => registrarVenues.Contains(r.VenueId))
                .OrderByDescending(r => r.Date)
                .ToList();

            var totalReviews = reviews.Count;
            var averageRating = totalReviews > 0 ? reviews.Average(r => r.Rating) : 0;
            var positiveReviews = reviews.Count(r => r.Rating >= 4);
            var negativeReviews = reviews.Count(r => r.Rating <= 2);

            var reviewsData = reviews.Select(r => new
            {
                CustomerName = r.UserName,
                Rating = r.Rating,
                Comment = r.Comment,
                Date = r.Date,
                VenueName = r.Venue.Name
            }).ToList();

            return new
            {
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = totalReviews,
                PositiveReviews = positiveReviews,
                NegativeReviews = negativeReviews,
                Reviews = reviewsData
            };
        }

        private Venue GetVenueById(int id)
        {
            return _context.Venues
                .Include(v => v.Registrar)
                .FirstOrDefault(v => v.Id == id) ?? new Venue();
        }

        [HttpGet]
        public IActionResult GetLatestStats()
        {
            var stats = new
            {
                venuesCount = GetRegistrarDashboardData().VenuesCount,
                activeBookingsCount = GetRegistrarDashboardData().ActiveBookingsCount,
                averageRating = GetRegistrarDashboardData().AverageRating,
                totalRevenue = GetRegistrarDashboardData().TotalRevenue
            };

            return Json(stats);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVenueStatus([FromBody] VenueStatusUpdateModel model)
        {
            try
            {
                var venue = await _context.Venues.FindAsync(model.VenueId);
                if (venue == null)
                {
                    return Json(new { success = false, message = "Venue not found" });
                }

                venue.IsActive = model.IsActive;
                venue.Status = model.IsActive ? "Active" : "Inactive";
                venue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating the venue status" });
            }
        }

        public class VenueStatusUpdateModel
        {
            public int VenueId { get; set; }
            public bool IsActive { get; set; }
        }
    }
}