using EventManagementSystem.Models;
using EventManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Linq;

namespace EventManagementSystem.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingDbService _bookingService;
        private readonly VenueService _venueService;

        public ReviewsController(ApplicationDbContext context, BookingDbService bookingService, VenueService venueService)
        {
            _context = context;
            _bookingService = bookingService;
            _venueService = venueService;
        }

        public class ReviewRequest
        {
            public int VenueId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
        }

        [HttpPost]
        public IActionResult Create([FromBody] ReviewRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { success = false, message = "User must be logged in to submit a review." });
            }

            var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            System.Diagnostics.Debug.WriteLine("Available claims:");
            foreach (var claim in allClaims)
            {
                System.Diagnostics.Debug.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }

            var venue = _venueService.GetVenueById(request.VenueId);
            if (venue == null)
            {
                return NotFound(new { success = false, message = "Venue not found." });
            }

            var userId = User.FindFirstValue("sub") ?? 
                        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                        User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ??
                               User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var userByEmail = _context.Users.FirstOrDefault(u => u.Email == userEmail);
                    if (userByEmail != null)
                    {
                        userId = userByEmail.Id.ToString();
                    }
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User ID not found. Please try logging out and logging in again." });
                }
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == int.Parse(userId));
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "User not found in the database." });
            }

            var review = new VenueReviewModel
            {
                VenueId = request.VenueId,
                UserId = user.Id,
                UserName = $"{user.FirstName} {user.LastName}",
                UserImage = user.ProfilePicture,
                Rating = request.Rating,
                Comment = request.Comment,
                Date = DateTime.Now
            };

            try
            {
                var existingReview = _context.VenueReviews
                    .FirstOrDefault(r => r.VenueId == request.VenueId && r.UserId == user.Id);
                
                if (existingReview != null)
                {
                    return BadRequest(new { success = false, message = "You have already reviewed this venue." });
                }

                _context.VenueReviews.Add(review);
                _context.SaveChanges();

                var venueReviews = _context.VenueReviews.Where(r => r.VenueId == venue.Id);
                venue.Rating = venueReviews.Average(r => r.Rating);
                venue.ReviewCount = venueReviews.Count();
                _context.SaveChanges();

                return Json(new { success = true, message = "Review submitted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while saving the review." });
            }
        }
    }
} 