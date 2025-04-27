using EventManagementSystem.Models;
using EventManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Claims;

namespace EventManagementSystem.Controllers
{
    public class BookingsController : Controller
    {
        private readonly BookingDbService _bookingService;
        private readonly VenueService _venueService;
        private readonly ApplicationDbContext _context;

        public BookingsController(BookingDbService bookingService, VenueService venueService, ApplicationDbContext context)
        {
            _bookingService = bookingService;
            _venueService = venueService;
            _context = context;
        }

        public IActionResult Index()
        {
            var bookings = _bookingService.GetAllBookings();
            return View(bookings);
        }

        public IActionResult Details(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }

        public IActionResult Create(int venueId)
        {
            var venue = _venueService.GetVenueById(venueId);
            if (venue == null)
            {
                return NotFound();
            }
            
            var booking = new Booking
            {
                VenueId = venueId,
                Venue = venue,
                Date = DateTime.Now.Date.AddDays(1),
                StartTime = "14:00",
                EndTime = "18:00",
                GuestCount = 50,
                CleaningFee = venue.CleaningFee,
                CustomerId = 1,
                CustomerName = "John Doe",
                CustomerEmail = "john.doe@example.com",
                CustomerPhone = "(555) 123-4567"
            };
            
            TimeSpan.TryParse(booking.StartTime, out var startTime);
            TimeSpan.TryParse(booking.EndTime, out var endTime);
            decimal venueTotal = venue.PricePerHour * (endTime.Hours - startTime.Hours);
            booking.ServiceFee = venueTotal * 0.1m;
            booking.TotalPrice = venueTotal + booking.ServiceFee + booking.CleaningFee;
            
            return View(booking);
        }

        [HttpPost]
        public IActionResult Create(Booking booking)
        {
            if (ModelState.IsValid)
            {
                _bookingService.AddBooking(booking);
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }
            
            booking.Venue = _venueService.GetVenueById(booking.VenueId);
            return View(booking);
        }

        public IActionResult Edit(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }

        [HttpPost]
        public IActionResult Edit(int id, Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _bookingService.UpdateBooking(booking);
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }
            return View(booking);
        }

        public IActionResult Delete(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _bookingService.DeleteBooking(id);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Confirm(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }
            booking.Status = "Confirmed";
            _bookingService.UpdateBooking(booking);
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }

        public IActionResult Cancel(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }
            booking.Status = "Cancelled";
            _bookingService.UpdateBooking(booking);
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }

        public IActionResult Complete(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }
            booking.Status = "Completed";
            _bookingService.UpdateBooking(booking);
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }

        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ProcessPayment([FromForm] Booking booking, string ServicesData)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    booking.CreatedAt = DateTime.Now;
                    booking.UpdatedAt = DateTime.Now;
                    booking.BookingDate = booking.Date; 
                    booking.StartTimeSpan = TimeSpan.Parse(booking.StartTime);
                    booking.EndTimeSpan = TimeSpan.Parse(booking.EndTime);
                    booking.Status = "Pending"; 
                    booking.PaymentMethod = "Credit Card";
                    booking.PaymentStatus = "Paid";
                    booking.PaymentDate = DateTime.Now;
                    booking.AdditionalServicesCost = 0; 
                    
                    var userIdClaim = User.FindFirst("UserId");
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        booking.CustomerId = userId;
                    }
                    if (!string.IsNullOrEmpty(ServicesData))
                    {
                        try
                        {
                            var servicesList = JsonSerializer.Deserialize<List<BookingService>>(ServicesData);
                            if (servicesList != null && servicesList.Count > 0)
                            {
                                foreach (var service in servicesList)
                                {
                                    if (string.IsNullOrEmpty(service.ImageUrl))
                                    {
                                        service.ImageUrl = $"/images/services/{service.ServiceType.ToLower()}/default.jpg";
                                    }
                                }
                                booking.Services = servicesList;
                                booking.AdditionalServicesCost = servicesList.Sum(s => s.ServicePrice * s.Quantity);
                                booking.TotalPrice += booking.AdditionalServicesCost;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing services data: {ex.Message}");
                        }
                    }

                    if (string.IsNullOrEmpty(booking.VenueName))
                    {
                        var venue = _venueService.GetVenueById(booking.VenueId);
                        if (venue != null)
                        {
                            booking.VenueName = venue.Name;
                        }
                    }

                    _bookingService.AddBooking(booking);
                    
                    return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
                }
                
               
                return View("Checkout", booking);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while processing your payment. Please try again.");
                return View("Checkout", booking);
            }
        }

        public IActionResult Confirmation(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
            {
                return NotFound();
            }
            
            return View(booking);
        }
    }
} 