using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace EventManagementSystem.Models
{
    public class Booking
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Venue")]
        public string VenueName { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        
        public DateTime BookingDate { get => Date; set => Date = value; }
        
        [Required]
        [Display(Name = "Start Time")]
        public string StartTime { get; set; }
        
        private TimeSpan _startTimeSpan;
        public TimeSpan StartTimeSpan
        {
            get 
            {
                if (TimeSpan.TryParse(StartTime, out var result))
                    return result;
                return _startTimeSpan;
            }
            set 
            { 
                _startTimeSpan = value;
                StartTime = $"{value.Hours:D2}:{value.Minutes:D2}";
            }
        }
        
        [Required]
        [Display(Name = "End Time")]
        public string EndTime { get; set; }
        
        private TimeSpan _endTimeSpan;
        public TimeSpan EndTimeSpan
        {
            get 
            {
                if (TimeSpan.TryParse(EndTime, out var result))
                    return result;
                return _endTimeSpan;
            }
            set 
            { 
                _endTimeSpan = value;
                EndTime = $"{value.Hours:D2}:{value.Minutes:D2}";
            }
        }
        
        [Required]
        [Display(Name = "Number of Guests")]
        [Range(1, 1000, ErrorMessage = "Number of guests must be between 1 and 1000")]
        public int GuestCount { get; set; }
        
        [Required]
        public string Status { get; set; }
        
        public int VenueId { get; set; }
        
        public int CustomerId { get; set; }
        
        public Venue? Venue { get; set; }
        
        public string? CustomerName { get; set; }
        
        public string? CustomerEmail { get; set; }
        
        public string? CustomerPhone { get; set; }
        
        public string? CustomerProfilePicture { get; set; } = "/images/users/default.jpg";
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        public decimal ServiceFee { get; set; }
        
        public decimal CleaningFee { get; set; }
        
        public decimal AdditionalServicesCost { get; set; } = 0;
        
        public List<BookingService> Services { get; set; } = new List<BookingService>();
        
        public string? EventTitle { get; set; }
        
        public string? EventType { get; set; }
        
        public string? SpecialRequests { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public string? PaymentMethod { get; set; }
        
        public string? PaymentStatus { get; set; } = "Pending";
        
        public DateTime? PaymentDate { get; set; }
        
        public string BookingReference => $"EH-{Id:D6}";

        public string? Email {
            get => CustomerEmail;
            set => CustomerEmail = value;
        }
        
        public decimal CalculateTotalWithServices()
        {
            decimal servicesTotal = 0;
            if (Services != null && Services.Count > 0)
            {
                servicesTotal = Services.Sum(s => s.TotalPrice);
            }
            AdditionalServicesCost = servicesTotal;
            return TotalPrice + servicesTotal;
        }
    }
} 