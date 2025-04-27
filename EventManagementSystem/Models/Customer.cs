using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Models
{
    public class Customer
    {
        public Customer()
        {
            Address = "";
            City = "";
            State = "";
            ZipCode = "";
            CustomerId = "";
            PreferredEventTypesString = "";
            DateOfBirth = DateTime.MinValue;
            
            UpcomingBookings = new List<Booking>();
            RecommendedVenues = new List<Venue>();
        }

        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        public string CustomerId { get; set; }
        
        [StringLength(200)]
        public string Address { get; set; }
        
        [StringLength(100)]
        public string City { get; set; }
        
        [StringLength(100)]
        public string State { get; set; }
        
        [Display(Name = "Zip Code")]
        [StringLength(20)]
        public string ZipCode { get; set; }
        
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }
        
        public int BookingsCount { get; set; }
        
        [NotMapped]
        public int TotalBookings
        {
            get => BookingsCount;
            set => BookingsCount = value;
        }
        
        public int FavoritesCount { get; set; }
        
        public int ReviewsCount { get; set; }
        
        [Display(Name = "Preferred Event Types")]
        public string PreferredEventTypesString { get; set; }
        
        [NotMapped]
        public List<string> PreferredEventTypes
        {
            get => string.IsNullOrEmpty(PreferredEventTypesString) 
                ? new List<string>() 
                : new List<string>(PreferredEventTypesString.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            set => PreferredEventTypesString = string.Join(", ", value ?? new List<string>());
        }
        
        [NotMapped]
        public List<Booking> UpcomingBookings { get; set; } = new List<Booking>();
        
        [NotMapped]
        public List<Venue> RecommendedVenues { get; set; } = new List<Venue>();
        
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
        [Display(Name = "Profile Picture")]
        public string ProfilePictureUrl
        {
            get => ProfilePicture ?? $"https://ui-avatars.com/api/?name={FirstName}+{LastName}&background=0284c7&color=fff";
            set => ProfilePicture = value;
        }
        
        [NotMapped]
        [Display(Name = "Member Since")]
        [DataType(DataType.Date)]
        public DateTime MemberSince
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }
        
        [NotMapped]
        public string Phone
        {
            get => PhoneNumber;
            set => PhoneNumber = value;
        }
        
        [NotMapped]
        public UserRole Role 
        { 
            get => User?.Role ?? UserRole.Customer; 
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
    }
} 