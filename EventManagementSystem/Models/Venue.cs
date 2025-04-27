using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace EventManagementSystem.Models
{
    public class Venue
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        public string Location { get; set; }
        
        public string Description { get; set; }
        
        [Range(0, 5)]
        public double Rating { get; set; }
        
        [Required]
        [Display(Name = "Price Per Hour")]
        [Range(0, 10000)]
        public decimal PricePerHour { get; set; }
        
        [Display(Name = "Image")]
        public string ImageUrl { get; set; }
        
        public string Amenities { get; set; }
        
        [Display(Name = "Maximum Capacity")]
        public int MaxCapacity { get; set; }
        
        public string Type { get; set; }
        
        public bool IsAvailable { get; set; }
        
        public string AvailableEventTypes { get; set; }
        
        [Required]
        public string? Address { get; set; }
        
        [Required]
        public int Capacity { get; set; }
        
        public int Size { get; set; } 
        
        public decimal CleaningFee { get; set; }
        
        public string? AdditionalImages { get; set; }
        
        public string VenueTypes { get; set; }
        
        public string Features { get; set; }
        
        public int ReviewCount { get; set; }
        
        public bool IsFeatured { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string? HostName { get; set; }
        
        public string? HostImage { get; set; }
        
        public DateTime HostMemberSince { get; set; }
        
        public int HostBookingCount { get; set; }
        
        public int HostResponseRate { get; set; }
        
        public int HostResponseTime { get; set; } 
        
        public virtual ICollection<VenueReviewModel> Reviews { get; set; } = new List<VenueReviewModel>();
        
        public List<Venue> SimilarVenues { get; set; } = new List<Venue>();
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public int RegistrarId { get; set; }
        
        [ForeignKey("RegistrarId")]
        public virtual Registrar? Registrar { get; set; }
        
        public string? ContactEmail { get; set; }
        
        public string? ContactPhone { get; set; }
        
        public string? VenueName => Name;
        public DateTime Date { get; set; } = DateTime.Now.Date.AddDays(1);
        public DateTime BookingDate => Date;
        public TimeSpan StartTime { get; set; } = new TimeSpan(14, 0, 0);
        public TimeSpan EndTime { get; set; } = new TimeSpan(18, 0, 0);
        public int GuestCount { get; set; } = 50;
        public string? Status { get; set; } = "Confirmed";
        public string? CustomerName { get; set; } = "John Doe";
        public string? CustomerProfilePicture { get; set; } = "/images/users/default.jpg";
        public string? Email { get; set; } = "customer@example.com";

        [NotMapped]
        public List<string> AmenitiesList
        {
            get => string.IsNullOrEmpty(Amenities) 
                ? new List<string>() 
                : new List<string>(Amenities.Split(','));
            set => Amenities = value != null ? string.Join(",", value) : "";
        }

        [NotMapped]
        public List<string> AvailableEventTypesList
        {
            get => string.IsNullOrEmpty(AvailableEventTypes) 
                ? new List<string>() 
                : new List<string>(AvailableEventTypes.Split(','));
            set => AvailableEventTypes = value != null ? string.Join(",", value) : "";
        }

        [NotMapped]
        public List<string> AdditionalImagesList
        {
            get => string.IsNullOrEmpty(AdditionalImages) 
                ? new List<string>() 
                : AdditionalImages.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            set => AdditionalImages = value != null && value.Any() ? string.Join(",", value) : "";
        }

        [NotMapped]
        public List<string> VenueTypesList
        {
            get => string.IsNullOrEmpty(VenueTypes) 
                ? new List<string>() 
                : new List<string>(VenueTypes.Split(','));
            set => VenueTypes = value != null ? string.Join(",", value) : "";
        }

        [NotMapped]
        public List<string> FeaturesList
        {
            get => string.IsNullOrEmpty(Features) 
                ? new List<string>() 
                : new List<string>(Features.Split(','));
            set => Features = value != null ? string.Join(",", value) : "";
        }
    }
    
    public class VenueReview
    {
        public int Id { get; set; }
        
        public int VenueId { get; set; }
        
        public int UserId { get; set; }
        
        public string? UserName { get; set; }
        
        public string? UserImage { get; set; }
        
        public int Rating { get; set; }
        
        public string? Comment { get; set; }
        
        public DateTime Date { get; set; } = DateTime.Now;

        public virtual Venue Venue { get; set; }
    }
} 