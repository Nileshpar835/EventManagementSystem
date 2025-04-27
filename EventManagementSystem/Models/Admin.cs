using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Models
{
    public class Admin
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        public string AdminId { get; set; }
        
        public string Department { get; set; }
        
        public string Position { get; set; }
        
        [Range(1, 5)]
        public int AccessLevel { get; set; } = 5;
        
        public DateTime LastActivity { get; set; } = DateTime.Now;
        
        public int ActionsPerformed { get; set; } = 0;

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
        public string ProfilePictureUrl
        {
            get => ProfilePicture ?? $"https://ui-avatars.com/api/?name={FirstName}+{LastName}&background=0284c7&color=fff";
            set => ProfilePicture = value;
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
            get => User?.Role ?? UserRole.Admin; 
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