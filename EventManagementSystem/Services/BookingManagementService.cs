using EventManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventManagementSystem.Services
{
    public class BookingManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly VenueService _venueService;

        public BookingManagementService(ApplicationDbContext context, VenueService venueService)
        {
            _context = context;
            _venueService = venueService;
        }

        public List<Booking> GetAllBookings()
        {
            return _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Services)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
        }

        public Booking GetBookingById(int id)
        {
            return _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Services)
                .FirstOrDefault(b => b.Id == id);
        }

        public List<Booking> GetBookingsByCustomerId(int customerId)
        {
            return _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Services)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
        }

        public List<Booking> GetBookingsByVenueId(int venueId)
        {
            return _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Services)
                .Where(b => b.VenueId == venueId)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
        }

        public List<Booking> GetBookingsByRegistrarId(int registrarId)
        {
            return _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Services)
                .Where(b => b.Venue.RegistrarId == registrarId)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
        }

        public List<Booking> GetUpcomingBookings()
        {
            var today = DateTime.Now.Date;
            return _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Services)
                .Where(b => b.Date >= today)
                .OrderBy(b => b.Date)
                .ThenBy(b => b.StartTime)
                .ToList();
        }

        public List<Booking> GetRecentBookings(int count = 5)
        {
            return _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Services)
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToList();
        }

        public void AddBooking(Booking booking)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    booking.CreatedAt = DateTime.Now;
                    booking.UpdatedAt = DateTime.Now;

                    _context.Bookings.Add(booking);
                    _context.SaveChanges();

                    if (booking.Services != null && booking.Services.Any())
                    {
                        foreach (var service in booking.Services)
                        {
                            service.BookingId = booking.Id;
                            _context.BookingServices.Add(service);
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void UpdateBooking(Booking booking)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    booking.UpdatedAt = DateTime.Now;

                    _context.Entry(booking).State = EntityState.Modified;

                    if (booking.Services != null)
                    {
                        var existingServices = _context.BookingServices
                            .Where(bs => bs.BookingId == booking.Id)
                            .ToList();

                        foreach (var existingService in existingServices)
                        {
                            if (!booking.Services.Any(s => s.Id == existingService.Id))
                            {
                                _context.BookingServices.Remove(existingService);
                            }
                        }

                        foreach (var service in booking.Services)
                        {
                            if (service.Id == 0)
                            {
                                service.BookingId = booking.Id;
                                _context.BookingServices.Add(service);
                            }
                            else
                            {
                                _context.Entry(service).State = EntityState.Modified;
                            }
                        }
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void DeleteBooking(int id)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var services = _context.BookingServices.Where(bs => bs.BookingId == id);
                    _context.BookingServices.RemoveRange(services);

                    var booking = _context.Bookings.Find(id);
                    if (booking != null)
                    {
                        _context.Bookings.Remove(booking);
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
} 