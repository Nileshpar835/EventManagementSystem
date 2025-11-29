using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using EventManagementSystem.Services;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly VenueService _venueService;
        private readonly FoodService _foodService;
        private readonly FlowerService _flowerService;
        private readonly LightService _lightService;

        public AdminController(
            ApplicationDbContext context,
            UserService userService,
            VenueService venueService,
            FoodService foodService,
            FlowerService flowerService,
            LightService lightService)
        {
            _context = context;
            _userService = userService;
            _venueService = venueService;
            _foodService = foodService;
            _flowerService = flowerService;
            _lightService = lightService;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
        
        private IActionResult AjaxOrNormalView(string viewName, object model = null)
        {
            if (IsAjaxRequest())
            {
                return PartialView(viewName, model);
            }
            
            return View(viewName, model);
        }
        
        private IActionResult AjaxRedirectToAction(string actionName, string controllerName = null)
        {
            if (IsAjaxRequest())
            {
                return Json(new { redirect = Url.Action(actionName, controllerName ?? "Admin") });
            }
            
            return RedirectToAction(actionName, controllerName ?? "Admin");
        }

        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return NotFound();
            }

            var admin = await _userService.GetAdminByUserId(user.Id);
            if (admin == null)
            {
                admin = new Admin
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Username = user.Username,
                    Department = "System Administration",
                    Position = "Administrator",
                    AccessLevel = 5,
                    LastActivity = DateTime.UtcNow,
                    AdminId = $"A{user.Id:D5}", 
                    ActionsPerformed = 0
                };
                
                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();
                
            }
            
            var totalUsers = await _context.Users.CountAsync();
            var totalVenues = await _context.Venues.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var totalRevenue = await _context.Bookings.SumAsync(b => b.TotalPrice);

            var previousMonth = DateTime.UtcNow.AddMonths(-1);
            var previousMonthUsers = await _context.Users.CountAsync(u => u.CreatedAt < previousMonth);
            var previousMonthVenues = await _context.Venues.CountAsync(v => v.CreatedAt < previousMonth);
            var previousMonthBookings = await _context.Bookings.CountAsync(b => b.BookingDate < previousMonth);
            var previousMonthRevenue = await _context.Bookings.Where(b => b.BookingDate < previousMonth).SumAsync(b => b.TotalPrice);


            var recentActivities = await (
                from b in _context.Bookings
                join u in _context.Users on b.CustomerId equals u.Id
                orderby b.BookingDate descending
                select new
                {
                    UserName = $"{u.FirstName} {u.LastName}",
                    UserImage = u.ProfilePicture,
                    Description = $"Booked {b.VenueName} for {b.EventType}",
                    Date = b.BookingDate
                })
                .Take(5)
                .ToListAsync();

            var venueActivities = await (
                from v in _context.Venues
                join r in _context.Registrars on v.RegistrarId equals r.Id
                join u in _context.Users on r.UserId equals u.Id
                orderby v.CreatedAt descending
                select new
                {
                    UserName = $"{u.FirstName} {u.LastName}",
                    UserImage = u.ProfilePicture,
                    Description = $"Added new venue: {v.Name}",
                    Date = v.CreatedAt
                })
                .Take(5)
                .ToListAsync();

            var allActivities = recentActivities.Select(a => new
            {
                a.UserName,
                a.UserImage,
                a.Description,
                TimeAgo = GetTimeAgo(a.Date)
            })
            .Concat(venueActivities.Select(a => new
            {
                a.UserName,
                a.UserImage,
                a.Description,
                TimeAgo = GetTimeAgo(a.Date)
            }))
            .OrderByDescending(a => a.TimeAgo)
            .Take(5)
            .ToList();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalVenues = totalVenues;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.RecentActivities = allActivities;
            
            return AjaxOrNormalView("Index", admin);
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            
            return dateTime.ToString("MMM dd, yyyy");
        }
        
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userService.GetAllUsers();
            return AjaxOrNormalView("ManageUsers", users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            return AjaxOrNormalView("EditUser", user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user, IFormFile? profilePicture)
        {
            if (ModelState.IsValid)
            {
                if (profilePicture != null && profilePicture.Length > 0)
                {
                    var fileName = Path.GetFileName(profilePicture.FileName);
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = $"{user.Id}_{DateTime.UtcNow.Ticks}{fileExtension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles", newFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }
                    
                    user.ProfilePicture = $"/images/profiles/{newFileName}";
                }
                
                await _userService.UpdateUser(user);
                return AjaxRedirectToAction(nameof(ManageUsers));
            }
            return AjaxOrNormalView("EditUser", user);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            await _userService.DeleteUser(id);
            return AjaxRedirectToAction(nameof(ManageUsers));
        }
        
        public IActionResult ManageVenues(bool showEmpty = false)
        {
            try
            {
                
                if (showEmpty)
                {
                    _venueService.SetEmptyListMode(true);
                }
                
                var venues = _venueService.GetAllVenues();
                
                if (showEmpty)
                {
                    _venueService.SetEmptyListMode(false);
                }
                
                return AjaxOrNormalView("ManageVenues", venues);
            }
            catch (Exception ex)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Error loading venues. Please try again." });
                }
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Error loading venues. Please try again."
                });
            }
        }
        
        [HttpGet]
        public IActionResult CreateVenue()
        {
            var venue = new Venue
            {
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Name = "",
                Location = "",
                Address = "",
                PricePerHour = 0,
                Capacity = 0,
                Description = "",
                ImageUrl = "/images/venues/default.jpg",
                Type = "Venue", 
                Features = "",
                Amenities = "",
                VenueTypes = "",
                AdditionalImages = "",
                AvailableEventTypes = "",
                HostName = "Host",
                HostImage = "/images/hosts/default.jpg",
                HostMemberSince = DateTime.UtcNow,
                HostBookingCount = 0,
                HostResponseRate = 100,
                HostResponseTime = 24,
                MaxCapacity = 0,
                IsAvailable = true,
                Status = "Active"
            };
            
            return View("EditVenue", venue);
        }
        
        [HttpPost]
        public IActionResult CreateVenue(Venue venue, IFormFile? mainImage, IFormFile[]? additionalImages, IFormFile? hostImage)
        {
            venue.AdditionalImages = "";
            venue.UpdatedAt = DateTime.UtcNow;
            venue.CreatedAt = DateTime.UtcNow;
            
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                    }
                }
            }
            
            if (ModelState.IsValid)
            {
                if (mainImage != null && mainImage.Length > 0)
                {
                    var fileName = Path.GetFileName(mainImage.FileName);
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = $"venue_{DateTime.UtcNow.Ticks}{fileExtension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "venues", newFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        mainImage.CopyTo(stream);
                    }
                    
                    venue.ImageUrl = $"/images/venues/{newFileName}";
                }
                else
                {
                    venue.ImageUrl = "/images/venues/default.jpg";
                }
                
                if (hostImage != null && hostImage.Length > 0)
                {
                    try
                    {
                        var fileName = Path.GetFileName(hostImage.FileName);
                        var fileExtension = Path.GetExtension(fileName);
                        var newFileName = $"host_{DateTime.Now.Ticks}{fileExtension}";
                        
                        var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "hosts");
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        var filePath = Path.Combine(directory, newFileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            hostImage.CopyTo(stream);
                        }
                        
                        venue.HostImage = $"/images/hosts/{newFileName}";
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("HostImage", "Failed to upload host image. Please try again.");
                    }
                }
                else
                {
                    venue.HostImage = "/images/hosts/default.jpg";
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
                            
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                image.CopyTo(stream);
                            }
                            
                            newImages.Add($"/images/venues/{newFileName}");
                        }
                    }
                    
                    venue.AdditionalImages = string.Join(",", newImages);
                }
                
                _venueService.AddVenue(venue);
                
                return RedirectToAction(nameof(ManageVenues));
            }
            
            return View("EditVenue", venue);
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
        public IActionResult EditVenue(Venue venue, IFormFile? mainImage, IFormFile[]? additionalImages, string[]? existingAdditionalImages, IFormFile? hostImage)
        {
            ModelState.Remove("AdditionalImages");
            
            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                    }
                }
            }
            
            if (ModelState.IsValid)
            {
                venue.IsActive = venue.Status == "Active";
                
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
                    
                    venue.ImageUrl = $"/images/venues/{newFileName}";
                }
                
                if (hostImage != null && hostImage.Length > 0)
                {
                    try
                    {
                        var fileName = Path.GetFileName(hostImage.FileName);
                        var fileExtension = Path.GetExtension(fileName);
                        var newFileName = $"host_{venue.Id}_{DateTime.Now.Ticks}{fileExtension}";
                        
                        var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "hosts");
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        var filePath = Path.Combine(directory, newFileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            hostImage.CopyTo(stream);
                        }
                        
                        venue.HostImage = $"/images/hosts/{newFileName}";
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("HostImage", "Failed to upload host image. Please try again.");
                    }
                }
                venue.AdditionalImages = venue.AdditionalImages ?? "";
                
                if (existingAdditionalImages != null && existingAdditionalImages.Length > 0)
                {
                    venue.AdditionalImages = string.Join(",", existingAdditionalImages);
                }
                
                if (additionalImages != null && additionalImages.Length > 0)
                {
                    var newImages = new List<string>();
                    
                    if (!string.IsNullOrEmpty(venue.AdditionalImages))
                    {
                        newImages.AddRange(venue.AdditionalImages.Split(','));
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
                    
                    venue.AdditionalImages = string.Join(",", newImages);
                }
                
                venue.UpdatedAt = DateTime.Now;
                
                if (venue.Id == 0)
                {
                    _venueService.AddVenue(venue);
                }
                else
                {
                    _venueService.UpdateVenue(venue);
                }
                
                return AjaxRedirectToAction(nameof(ManageVenues));
            }
            
            return View(venue);
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
        
        [HttpPost, ActionName("DeleteVenueConfirmed")]
        public IActionResult DeleteVenueConfirmed(int id)
        {
            _venueService.DeleteVenue(id);
            return AjaxRedirectToAction(nameof(ManageVenues));
        }
        
        [HttpGet]
        public IActionResult VenueDetails(int id)
        {
            var venue = _venueService.GetVenueById(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }

        [HttpPost]
        public IActionResult ToggleVenueStatus(int id)
        {
            try
            {
                var venue = _venueService.GetVenueById(id);
                if (venue == null)
                {
                    return Json(new { success = false, message = "Venue not found" });
                }

                venue.IsActive = !venue.IsActive;
                venue.Status = venue.IsActive ? "Active" : "Inactive";
                venue.UpdatedAt = DateTime.Now;

                _venueService.UpdateVenue(venue);

                return Json(new { 
                    success = true, 
                    message = $"Venue {(venue.IsActive ? "activated" : "deactivated")} successfully",
                    isActive = venue.IsActive 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating venue status" });
            }
        }
        
        public IActionResult Reports()
        {
            var totalUsers = _context.Users.Count();
            ViewBag.TotalUsers = totalUsers;
            
            var totalVenues = _context.Venues.Count();
            ViewBag.TotalVenues = totalVenues;
            
            var totalBookings = _context.Bookings.Count();
            ViewBag.TotalBookings = totalBookings;
            
            var totalRevenue = _context.Bookings.Sum(b => b.TotalPrice);
            ViewBag.TotalRevenue = totalRevenue;
            
            return AjaxOrNormalView("Reports");
        }
        
        public IActionResult Settings()
        {
            return AjaxOrNormalView("Settings");
        }
        
        public IActionResult ManageFood()
        {
            var foods = _foodService.GetAllFoods();
            return AjaxOrNormalView("ManageFood", foods);
        }
        
        [HttpGet]
        public IActionResult CreateFood()
        {
            var food = new Food
            {
                IsAvailable = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            
            return View("EditFood", food);
        }
        
        [HttpGet]
        public IActionResult EditFood(int id)
        {
            var food = _foodService.GetFoodById(id);
            if (food == null)
            {
                return NotFound();
            }
            return View(food);
        }
        
        [HttpPost]
        public IActionResult EditFood(Food food, IFormFile? foodImage)
        {
            
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                    }
                }
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    if (foodImage != null && foodImage.Length > 0)
                    {
                        var fileName = Path.GetFileName(foodImage.FileName);
                        var fileExtension = Path.GetExtension(fileName);
                        var newFileName = $"food_{food.Id}_{DateTime.Now.Ticks}{fileExtension}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "foods", newFileName);
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            foodImage.CopyTo(stream);
                        }
                        
                        food.ImageUrl = $"/images/foods/{newFileName}";
                    }
                    
                    food.UpdatedAt = DateTime.Now;
                    
                    if (food.Id == 0)
                    {
                        _foodService.AddFood(food);
                    }
                    else
                    {
                        _foodService.UpdateFood(food);
                    }
                    
                    return AjaxRedirectToAction(nameof(ManageFood));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the food item. Please try again.");
                }
            }
            
            return View(food);
        }
        
        [HttpGet]
        public IActionResult DeleteFood(int id)
        {
            var food = _foodService.GetFoodById(id);
            if (food == null)
            {
                return NotFound();
            }
            return View(food);
        }
        
        [HttpPost, ActionName("DeleteFood")]
        public IActionResult DeleteFoodConfirmed(int id)
        {
            _foodService.DeleteFood(id);
            return AjaxRedirectToAction(nameof(ManageFood));
        }
        
        public IActionResult ManageFlowers()
        {
            var flowers = _flowerService.GetAllFlowers();
            return AjaxOrNormalView("ManageFlowers", flowers);
        }
        
        [HttpGet]
        public IActionResult CreateFlower()
        {
            var flower = new Flower
            {
                IsAvailable = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Name = "",
                Description = "",
                Type = "",
                Price = 0.01m,
                Color = "",
                ImageUrl = "/images/flowers/default.jpg"
            };
            
            return View("EditFlower", flower);
        }
        
        [HttpGet]
        public IActionResult EditFlower(int id)
        {
            var flower = _flowerService.GetFlowerById(id);
            if (flower == null)
            {
                return NotFound();
            }
            return View(flower);
        }
        
        [HttpPost]
        public IActionResult EditFlower(Flower flower, IFormFile? flowerImage)
        {
            
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    
                    if (flowerImage != null && flowerImage.Length > 0)
                    {
                        var fileName = Path.GetFileName(flowerImage.FileName);
                        var fileExtension = Path.GetExtension(fileName);
                        var newFileName = $"flower_{flower.Id}_{DateTime.Now.Ticks}{fileExtension}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "flowers", newFileName);
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            flowerImage.CopyTo(stream);
                        }
                        
                        flower.ImageUrl = $"/images/flowers/{newFileName}";
                    }
                    else if (string.IsNullOrEmpty(flower.ImageUrl))
                    {
                        flower.ImageUrl = "/images/flowers/default.jpg";
                    }
                    
                    flower.UpdatedAt = DateTime.Now;
                    
                    if (flower.Id == 0)
                    {
                        _flowerService.AddFlower(flower);
                    }
                    else
                    {
                        _flowerService.UpdateFlower(flower);
                    }
                    
                    return AjaxRedirectToAction(nameof(ManageFlowers));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the flower. Please try again.");
                }
            }
            
            return View(flower);
        }
        
        [HttpGet]
        public IActionResult DeleteFlower(int id)
        {
            var flower = _flowerService.GetFlowerById(id);
            if (flower == null)
            {
                return NotFound();
            }
            return View(flower);
        }
        
        [HttpPost, ActionName("DeleteFlower")]
        public IActionResult DeleteFlowerConfirmed(int id)
        {
            _flowerService.DeleteFlower(id);
            return AjaxRedirectToAction(nameof(ManageFlowers));
        }
        
        public IActionResult ManageLights()
        {
            var lights = _lightService.GetAllLights();
            return AjaxOrNormalView("ManageLights", lights);
        }
        
        [HttpGet]
        public IActionResult CreateLight()
        {
            var light = new Light
            {
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Name = "",
                Description = "",
                Type = "",
                PricePerDay = 0.01m,
                Quantity = 1,
                Color = "",
                Power = 0,
                ImageUrl = "/images/lights/default.jpg"
            };
            
            return View("EditLight", light);
        }
        
        [HttpGet]
        public IActionResult EditLight(int id)
        {
            var light = _lightService.GetLightById(id);
            if (light == null)
            {
                return NotFound();
            }
            return View(light);
        }
        
        [HttpPost]
        public IActionResult EditLight(Light light, IFormFile? lightImage)
        {
            try
            {

                if (light.Id == 0 || string.IsNullOrEmpty(light.ImageUrl))
                {
                    light.ImageUrl = "/images/lights/default.jpg";
                }

                ModelState.Remove("ImageUrl");

                if (string.IsNullOrEmpty(light.Name))
                {
                    ModelState.AddModelError("Name", "Name is required");
                }
                if (string.IsNullOrEmpty(light.Type))
                {
                    ModelState.AddModelError("Type", "Type is required");
                }
                if (string.IsNullOrEmpty(light.Color))
                {
                    ModelState.AddModelError("Color", "Color is required");
                }
                if (light.PricePerDay <= 0)
                {
                    ModelState.AddModelError("PricePerDay", "Price must be greater than 0");
                }
                if (light.Quantity <= 0)
                {
                    ModelState.AddModelError("Quantity", "Quantity must be greater than 0");
                }
                if (light.Power < 0)
                {
                    ModelState.AddModelError("Power", "Power cannot be negative");
                }

                if (!ModelState.IsValid)
                {
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                    }
                    return View(light);
                }

                if (lightImage != null && lightImage.Length > 0)
                {
                    var fileName = Path.GetFileName(lightImage.FileName);
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = $"light_{light.Id}_{DateTime.Now.Ticks}{fileExtension}";
                    
                    var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "lights");
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    var filePath = Path.Combine(directory, newFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        lightImage.CopyTo(stream);
                    }
                    
                    light.ImageUrl = $"/images/lights/{newFileName}";
                }
                
                if (light.Id == 0)
                {
                    _lightService.AddLight(light);
                }
                else
                {
                    _lightService.UpdateLight(light);
                }
                
                return AjaxRedirectToAction(nameof(ManageLights));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving the light. Please try again.");
                return View(light);
            }
        }
        
        [HttpGet]
        public IActionResult DeleteLight(int id)
        {
            var light = _lightService.GetLightById(id);
            if (light == null)
            {
                return NotFound();
            }
            return View(light);
        }
        
        [HttpPost, ActionName("DeleteLight")]
        public IActionResult DeleteLightConfirmed(int id)
        {
            _lightService.DeleteLight(id);
            return AjaxRedirectToAction(nameof(ManageLights));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user, IFormFile? profilePicture)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email || u.Username == user.Username);

                if (existingUser != null)
                {
                    if (existingUser.Email == user.Email)
                    {
                        ModelState.AddModelError("Email", "Email is already taken.");
                    }
                    if (existingUser.Username == user.Username)
                    {
                        ModelState.AddModelError("Username", "Username is already taken.");
                    }
                    return View(user);
                }

                if (profilePicture != null && profilePicture.Length > 0)
                {
                    var fileName = Path.GetFileName(profilePicture.FileName);
                    var fileExtension = Path.GetExtension(fileName);
                    var newFileName = $"{DateTime.Now.Ticks}{fileExtension}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles", newFileName);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }
                    
                    user.ProfilePicture = $"/images/profiles/{newFileName}";
                }

                user.Password = Models.User.HashPassword(user.Password);
                
                await _userService.AddUser(user);
                await _context.SaveChangesAsync();

                if (user.Role == UserRole.Customer)
                {
                    var customer = new Customer
                    {
                        UserId = user.Id,
                        CustomerId = "C" + user.Id.ToString("D5")
                    };
                    _context.Customers.Add(customer);
                }
                else if (user.Role == UserRole.Registrar)
                {
                    var registrar = new Registrar
                    {
                        UserId = user.Id,
                        RegistrarId = "R" + user.Id.ToString("D5")
                    };
                    _context.Registrars.Add(registrar);
                }
                else if (user.Role == UserRole.Admin)
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
                return AjaxRedirectToAction(nameof(ManageUsers));
            }

            return View(user);
        }

        public async Task<IActionResult> ManageEquipment()
        {
            try
            {
                var equipments = await _context.Equipment.ToListAsync();
                return AjaxOrNormalView("ManageEquipment", equipments);
            }
            catch (Exception ex)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Error loading equipment. Please try again." });
                }
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Error loading equipment. Please try again."
                });
            }
        }
        
        public IActionResult CreateEquipment()
        {
            var equipment = new Equipment
            {
                IsAvailable = true,
                ImageUrl = "/images/equipment/default.jpg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Name = "",
                Description = "",
                Category = "",
                PricePerDay = 0.01m,
                Quantity = 1
            };
            return View("EditEquipment", equipment);
        }
        
        [HttpGet]
        public async Task<IActionResult> EditEquipment(int id)
        {
            var equipment = await _context.Equipment.FindAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }
            return View(equipment);
        }
        
        [HttpPost]
        public async Task<IActionResult> EditEquipment(Equipment equipment, IFormFile? equipmentImage)
        {
            
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (equipmentImage != null && equipmentImage.Length > 0)
                    {
                        var fileName = Path.GetFileName(equipmentImage.FileName);
                        var fileExtension = Path.GetExtension(fileName);
                        var newFileName = $"equipment_{equipment.Id}_{DateTime.Now.Ticks}{fileExtension}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "equipment", newFileName);
                        
                        var directoryPath = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await equipmentImage.CopyToAsync(stream);
                        }
                        
                        equipment.ImageUrl = $"/images/equipment/{newFileName}";
                    }
                    else if (equipment.Id == 0)
                    {
                        equipment.ImageUrl = "/images/equipment/default.jpg";
                    }
                    
                    equipment.UpdatedAt = DateTime.UtcNow;
                    
                    if (equipment.Id == 0)
                    {
                        equipment.CreatedAt = DateTime.UtcNow;
                        _context.Equipment.Add(equipment);
                    }
                    else
                    {
                        _context.Equipment.Update(equipment);
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    return AjaxRedirectToAction(nameof(ManageEquipment));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the equipment. Please try again.");
                }
            }
            
            return View(equipment);
        }
        
        [HttpGet]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.Equipment.FindAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }
            return View(equipment);
        }
        
        [HttpPost, ActionName("DeleteEquipment")]
        public async Task<IActionResult> DeleteEquipmentConfirmed(int id)
        {
            try
            {
                var equipment = await _context.Equipment.FindAsync(id);
                if (equipment != null)
                {
                    _context.Equipment.Remove(equipment);
                    await _context.SaveChangesAsync();
                }
                return AjaxRedirectToAction(nameof(ManageEquipment));
            }
            catch (Exception ex)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Error deleting equipment. Please try again." });
                }
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Error deleting equipment. Please try again."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            object roleInfo = null;
            if (user.Role == UserRole.Admin)
            {
                roleInfo = await _userService.GetAdminByUserId(user.Id);
            }
            else if (user.Role == UserRole.Registrar)
            {
                roleInfo = await _userService.GetRegistrarByUserId(user.Id);
            }
            else if (user.Role == UserRole.Customer)
            {
                roleInfo = await _userService.GetCustomerByUserId(user.Id);
            }

            ViewBag.RoleInfo = roleInfo;
            return AjaxOrNormalView("UserDetails", user);
        }

        public async Task<IActionResult> Profile()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return NotFound();
            }

            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (admin == null)
            {
                admin = new Admin
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Username = user.Username,
                    Department = "System Administration",
                    Position = "Administrator",
                    AccessLevel = 5,
                    LastActivity = DateTime.UtcNow,
                    AdminId = $"A{user.Id:D5}", 
                    User = user 
                };
                
                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();
                
            }

            return View(admin);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(Admin model, IFormFile profilePicture)
        {

            ModelState.Remove("User.Password");
            ModelState.Remove("User.PhoneNumber");
            ModelState.Remove("User.ProfilePicture");
            ModelState.Remove("User.Role");
            ModelState.Remove("ProfilePicture");

            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                    }
                }
                return View("Profile", model);
            }

            try
            {
                var username = User.Identity.Name;

                var userToUpdate = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (userToUpdate == null)
                {
                    return NotFound();
                }


                userToUpdate.FirstName = model.User.FirstName;
                userToUpdate.LastName = model.User.LastName;
                userToUpdate.Email = model.User.Email;

                if (profilePicture != null && profilePicture.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                    
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    var uniqueFileName = $"{DateTime.UtcNow.Ticks}_{Path.GetFileName(profilePicture.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }
                    
                    userToUpdate.ProfilePicture = $"/images/profiles/{uniqueFileName}";
                }

                _context.Users.Update(userToUpdate);
                await _context.SaveChangesAsync();

                var adminToUpdate = await _context.Admins
                    .FirstOrDefaultAsync(a => a.UserId == userToUpdate.Id);

                if (adminToUpdate == null)
                {
                    return NotFound();
                }


                adminToUpdate.Department = model.Department;
                adminToUpdate.Position = model.Position;
                adminToUpdate.LastActivity = DateTime.UtcNow;

                _context.Admins.Update(adminToUpdate);
                await _context.SaveChangesAsync();


                var updatedAdmin = await _context.Admins
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.UserId == userToUpdate.Id);

                return View("Profile", updatedAdmin);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving your profile. Please try again.");
                return View("Profile", model);
            }
        }

        [HttpGet]
        public IActionResult GetLatestStats()
        {
            if (IsAjaxRequest())
            {
                try
                {
                    var totalUsers = _context.Users.Count();
                    var totalVenues = _context.Venues.Count();
                    var totalBookings = _context.Bookings.Count();
                    var totalRevenue = _context.Bookings.Sum(b => b.TotalPrice);
                    var previousMonth = DateTime.UtcNow.AddMonths(-1);
                    var previousMonthUsers = _context.Users.Count(u => u.CreatedAt < previousMonth);
                    var previousMonthVenues = _context.Venues.Count(v => v.CreatedAt < previousMonth);
                    var previousMonthBookings = _context.Bookings.Count(b => b.BookingDate < previousMonth);
                    var previousMonthRevenue = _context.Bookings.Where(b => b.BookingDate < previousMonth).Sum(b => b.TotalPrice);
                    var userIncrease = previousMonthUsers == 0 ? 100 : ((totalUsers - previousMonthUsers) * 100.0m / previousMonthUsers);
                    var venueIncrease = previousMonthVenues == 0 ? 100 : ((totalVenues - previousMonthVenues) * 100.0m / previousMonthVenues);
                    var bookingIncrease = previousMonthBookings == 0 ? 100 : ((totalBookings - previousMonthBookings) * 100.0m / previousMonthBookings);
                    var revenueIncrease = previousMonthRevenue == 0 ? 100 : ((totalRevenue - previousMonthRevenue) * 100.0m / previousMonthRevenue);
                    var recentBookings = _context.Bookings
                        .OrderByDescending(b => b.BookingDate)
                        .Take(5)
                        .Select(b => new { 
                            id = b.Id,
                            customerName = b.CustomerName,
                            venueName = b.VenueName,
                            date = b.BookingDate.ToString("yyyy-MM-dd")
                        })
                        .ToList();

                    return Json(new { 
                        userCount = totalUsers,
                        venueCount = totalVenues,
                        bookingCount = totalBookings,
                        revenue = totalRevenue,
                        userIncrease = Math.Round(userIncrease, 1),
                        venueIncrease = Math.Round(venueIncrease, 1),
                        bookingIncrease = Math.Round(bookingIncrease, 1),
                        revenueIncrease = Math.Round(revenueIncrease, 1),
                        recentBookings = recentBookings
                    });
                }
                catch (Exception ex)
                {
                    return Json(new { 
                        error = "Error fetching statistics", 
                        userCount = 0, 
                        venueCount = 0, 
                        bookingCount = 0,
                        revenue = 0,
                        revenueIncrease = 0
                    });
                }
            }
            
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentPassword))
                {
                    TempData["ErrorMessage"] = "Current password is required.";
                    return RedirectToAction(nameof(Profile));
                }

                if (string.IsNullOrEmpty(NewPassword))
                {
                    TempData["ErrorMessage"] = "New password is required.";
                    return RedirectToAction(nameof(Profile));
                }

                if (string.IsNullOrEmpty(ConfirmPassword))
                {
                    TempData["ErrorMessage"] = "Please confirm your new password.";
                    return RedirectToAction(nameof(Profile));
                }

                if (NewPassword.Length < 6)
                {
                    TempData["ErrorMessage"] = "New password must be at least 6 characters long.";
                    return RedirectToAction(nameof(Profile));
                }

                if (NewPassword != ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "New passwords do not match.";
                    return RedirectToAction(nameof(Profile));
                }

                var username = User.Identity.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Account");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
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

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while changing your password. Please try again.";
                return RedirectToAction(nameof(Profile));
            }
        }
        
    }
} 