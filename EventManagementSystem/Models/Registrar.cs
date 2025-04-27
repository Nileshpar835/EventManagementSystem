using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Models
{
    public class Registrar
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        public string RegistrarId { get; set; }
        
        public string? OrganizationName { get; set; }
        
        [NotMapped]
        public string? CompanyName { 
            get => OrganizationName; 
            set => OrganizationName = value; 
        }
        
        public string? Position { get; set; }
        
        public string? BusinessPhone { get; set; }
        
        public string? BusinessAddress { get; set; }
        
        public string? BusinessWebsite { get; set; }
        
        public string? BusinessEmail { get; set; }
        
        public string? TaxId { get; set; }
        
        public bool IsApproved { get; set; }
        
        [NotMapped]
        public bool IsVerified {
            get => IsApproved;
            set => IsApproved = value;
        }
        
        public DateTime? ApprovalDate { get; set; }
        
        [NotMapped]
        public DateTime? VerificationDate {
            get => ApprovalDate;
            set => ApprovalDate = value;
        }
        
        public int VenuesCount { get; set; } = 0;
        
        [NotMapped]
        public int VenuesRegistered {
            get => VenuesCount;
            set => VenuesCount = value;
        }
        
        public int BookingsCount { get; set; } = 0;
        
        public int ActiveBookingsCount { get; set; } = 0;
        
        public decimal TotalRevenue { get; set; } = 0;
        
        public double AverageRating { get; set; } = 0;
        
        public string VenueSpecialties { get; set; } = "Wedding";
        
        [NotMapped]
        public string Email 
        { 
            get => User?.Email; 
            set { if (User != null) User.Email = value; } 
        }
        
        [NotMapped]
        public string Username 
        { 
            get => User?.Username; 
            set { if (User != null) User.Username = value; } 
        }
        
        [NotMapped]
        public string FirstName 
        { 
            get => User?.FirstName; 
            set { if (User != null) User.FirstName = value; } 
        }
        
        [NotMapped]
        public string LastName 
        { 
            get => User?.LastName; 
            set { if (User != null) User.LastName = value; } 
        }
        
        [NotMapped]
        public string PhoneNumber 
        { 
            get => User?.PhoneNumber; 
            set { if (User != null) User.PhoneNumber = value; } 
        }
        
        [NotMapped]
        public string ProfilePicture 
        { 
            get => User?.ProfilePicture; 
            set { if (User != null) User.ProfilePicture = value; } 
        }
        
        [NotMapped]
        public List<Booking> RecentBookings { get; set; } = new List<Booking>();
        
        [NotMapped]
        public string ProfilePictureUrl {
            get => ProfilePicture ?? "/images/registrars/default.jpg";
            set => ProfilePicture = value;
        }
        
        [NotMapped]
        public DateTime MemberSince { get; set; } = DateTime.Now.AddYears(-2);
        
        [NotMapped]
        public string? Phone { 
            get => PhoneNumber;
            set => PhoneNumber = value;
        }
        
        [NotMapped]
        public string? Name => $"{FirstName} {LastName}";
        
        [NotMapped]
        public string Organization { 
            get => OrganizationName;
            set => OrganizationName = value;
        }
        
        [NotMapped]
        public List<string> VenueCategories { get; set; } = new List<string> { "Wedding Hall", "Conference Center", "Banquet Hall" };
        
        [NotMapped]
        public List<Venue> Venues { get; set; } = new List<Venue>();
        
        [NotMapped]
        public UserRole Role 
        { 
            get => User?.Role ?? UserRole.Registrar; 
            set { if (User != null) User.Role = value; } 
        }
        
        [NotMapped]
        public DateTime CreatedAt 
        { 
            get => User?.CreatedAt ?? DateTime.Now; 
            set { if (User != null) User.CreatedAt = value; } 
        }
        
        [NotMapped]
        public DateTime? LastLogin 
        { 
            get => User?.LastLogin; 
            set { if (User != null) User.LastLogin = value; } 
        }
        
        [NotMapped]
        public string Password 
        { 
            get => User?.Password; 
            set { if (User != null) User.Password = value; } 
        }

        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
    }
} 