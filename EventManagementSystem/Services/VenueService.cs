using EventManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Services
{
    public class VenueService
    {
        private readonly ApplicationDbContext _context;
        private bool _returnEmptyList = false;

        public VenueService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Venue> GetAllVenues()
        {
            if (_returnEmptyList)
                return new List<Venue>();
                
            return _context.Venues
                .Include(v => v.Reviews)
                .Include(v => v.SimilarVenues)
                .ToList();
        }

        public List<Venue> GetFeaturedVenues()
        {
            return _context.Venues
                .Where(v => v.IsFeatured)
                .Include(v => v.Reviews)
                .Include(v => v.SimilarVenues)
                .ToList();
        }

        public Venue GetVenueById(int id)
        {
            return _context.Venues
                .Include(v => v.Reviews)
                .Include(v => v.SimilarVenues)
                .FirstOrDefault(v => v.Id == id);
        }

        public List<Venue> SearchVenues(string query, string location = null, int? capacity = null, string venueType = null, string priceRange = null)
        {
            var venues = _context.Venues.AsQueryable();
            
            if (!string.IsNullOrEmpty(query))
            {
                venues = venues.Where(v => 
                    v.Name.Contains(query) || 
                    v.Description.Contains(query));
            }
            
            if (!string.IsNullOrEmpty(location))
            {
                venues = venues.Where(v => v.Location.Contains(location));
            }
            
            if (capacity.HasValue)
            {
                venues = venues.Where(v => v.Capacity >= capacity.Value);
            }
            
            if (!string.IsNullOrEmpty(venueType))
            {
                venues = venues.Where(v => v.VenueTypes.Contains(venueType));
            }
            
            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange.ToLower())
                {
                    case "low": 
                        venues = venues.Where(v => v.PricePerHour < 2000);
                        break;
                    case "medium":
                        venues = venues.Where(v => v.PricePerHour >= 2000 && v.PricePerHour <= 4000);
                        break;
                    case "high": 
                        venues = venues.Where(v => v.PricePerHour > 4000);
                        break;
                }
            }
            
            return venues
                .Include(v => v.Reviews)
                .Include(v => v.SimilarVenues)
                .ToList();
        }
        
        public List<Venue> GetRelatedVenues(int venueId, int count = 3)
        {
            var venue = GetVenueById(venueId);
            if (venue == null) return new List<Venue>();
            
            return _context.Venues
                .Where(v => v.Id != venueId && 
                           (v.Location == venue.Location || 
                            v.VenueTypes == venue.VenueTypes))
                .OrderByDescending(v => v.Rating)
                .Take(count)
                .Include(v => v.Reviews)
                .Include(v => v.SimilarVenues)
                .ToList();
        }
        
        public List<Venue> GetPopularVenues(int count = 4)
        {
            return _context.Venues
                .OrderByDescending(v => v.Rating * v.ReviewCount)
                .Take(count)
                .Include(v => v.Reviews)
                .Include(v => v.SimilarVenues)
                .ToList();
        }
        
        public List<Venue> GetVenuesByRegistrarId(int registrarId)
        {
            return _context.Venues
                .Where(v => v.RegistrarId == registrarId)
                .Include(v => v.Reviews)
                .Include(v => v.SimilarVenues)
                .ToList();
        }
        
        public List<Venue> GetRecommendedVenuesForCustomer(Customer customer, int count = 4)
        {
            if (customer == null || string.IsNullOrEmpty(customer.PreferredEventTypesString))
            {
                return GetPopularVenues(count);
            }

            var customerPreferences = customer.PreferredEventTypes;
            
            var recommendedVenues = _context.Venues
                .Where(v => v.IsActive && 
                           customerPreferences.Any(pref => 
                               v.VenueTypes.Contains(pref) || 
                               v.AvailableEventTypes.Contains(pref)))
                .Include(v => v.Reviews)
                .Include(v => v.SimilarVenues)
                .OrderByDescending(v => 
                    (v.Rating * 0.4) + 
                    (v.ReviewCount * 0.2) + 
                    (customerPreferences.Count(pref => 
                        v.VenueTypes.Contains(pref) || 
                        v.AvailableEventTypes.Contains(pref)) * 0.4)) 
                .Take(count)
                .ToList();

            if (recommendedVenues.Count < count)
            {
                var remainingCount = count - recommendedVenues.Count;
                var popularVenues = GetPopularVenues(remainingCount * 2)
                    .Where(v => !recommendedVenues.Any(rv => rv.Id == v.Id))
                    .Take(remainingCount);
                recommendedVenues.AddRange(popularVenues);
            }

            return recommendedVenues;
        }
        
        public void AddVenue(Venue venue)
        {
            try
            {
                if (venue == null)
                {
                    throw new ArgumentNullException(nameof(venue), "Venue cannot be null");
                }

                if (string.IsNullOrEmpty(venue.Name))
                {
                    throw new ArgumentException("Venue name is required", nameof(venue));
                }

                if (string.IsNullOrEmpty(venue.Location))
                {
                    throw new ArgumentException("Venue location is required", nameof(venue));
                }

                if (string.IsNullOrEmpty(venue.Address))
                {
                    throw new ArgumentException("Venue address is required", nameof(venue));
                }

                if (venue.Capacity <= 0)
                {
                    throw new ArgumentException("Venue capacity must be greater than 0", nameof(venue));
                }

                if (venue.PricePerHour <= 0)
                {
                    throw new ArgumentException("Price per hour must be greater than 0", nameof(venue));
                }

                if (string.IsNullOrEmpty(venue.Features))
                {
                    venue.Features = "";
                }

                if (string.IsNullOrEmpty(venue.VenueTypes))
                {
                    venue.VenueTypes = "";
                }

                if (string.IsNullOrEmpty(venue.AvailableEventTypes))
                {
                    venue.AvailableEventTypes = venue.VenueTypes;
                }

                if (string.IsNullOrEmpty(venue.AdditionalImages))
                {
                    venue.AdditionalImages = "";
                }

                if (string.IsNullOrEmpty(venue.Description))
                {
                    venue.Description = "";
                }

                if (string.IsNullOrEmpty(venue.Type))
                {
                    venue.Type = "Venue";
                }

                if (venue.CreatedAt == default)
                {
                    venue.CreatedAt = DateTime.Now;
                }

                if (venue.UpdatedAt == default)
                {
                    venue.UpdatedAt = DateTime.Now;
                }

                if (string.IsNullOrEmpty(venue.ImageUrl))
                {
                    venue.ImageUrl = "/images/venues/default.jpg";
                }

                if (string.IsNullOrEmpty(venue.HostName))
                {
                    venue.HostName = "Host";
                }

                if (string.IsNullOrEmpty(venue.HostImage))
                {
                    venue.HostImage = "/images/hosts/default.jpg";
                }

                if (venue.HostMemberSince == default)
                {
                    venue.HostMemberSince = DateTime.Now;
                }

                venue.IsActive = true;
                venue.IsAvailable = true;
                venue.Rating = 0;
                venue.ReviewCount = 0;
                venue.HostBookingCount = 0;
                venue.HostResponseRate = 100;
                venue.HostResponseTime = 24;
                venue.Status = "Active";

                _context.Venues.Add(venue);
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding venue: {ex.Message}");
                throw; 
            }
        }
        
        public void UpdateVenue(Venue venue)
        {
            try
            {
                var existingVenue = _context.Venues
                    .Include(v => v.Reviews)
                    .Include(v => v.SimilarVenues)
                    .FirstOrDefault(v => v.Id == venue.Id);

                if (existingVenue != null)
                {
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
                    existingVenue.ImageUrl = venue.ImageUrl;
                    existingVenue.AdditionalImages = venue.AdditionalImages;
                    existingVenue.UpdatedAt = DateTime.Now;
                    existingVenue.HostImage = venue.HostImage;
                    existingVenue.HostName = venue.HostName;
                    existingVenue.HostMemberSince = venue.HostMemberSince;
                    existingVenue.HostBookingCount = venue.HostBookingCount;
                    existingVenue.HostResponseRate = venue.HostResponseRate;
                    existingVenue.HostResponseTime = venue.HostResponseTime;

                    _context.Entry(existingVenue).State = EntityState.Modified;

                    var result = _context.SaveChanges();
                    
                    if (result > 0)
                    {
                        Console.WriteLine($"Venue updated successfully: ID {venue.Id}, Name: {venue.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"No changes were made to venue: ID {venue.Id}");
                    }
                }
                else
                {
                    throw new Exception($"Venue with ID {venue.Id} not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating venue: {ex.Message}");
                throw;
            }
        }
        
        public void DeleteVenue(int id)
        {
            var venue = _context.Venues.Find(id);
            if (venue != null)
            {
                _context.Venues.Remove(venue);
                _context.SaveChanges();
            }
        }

        public void SetEmptyListMode(bool isEmpty)
        {
            _returnEmptyList = isEmpty;
        }

        public List<VenueReviewModel> GetLatestReviews(int count = 3)
        {
            return _context.VenueReviews
                .Include(r => r.Venue)
                .OrderByDescending(r => r.Date)
                .Take(count)
                .ToList();
        }
    }
} 