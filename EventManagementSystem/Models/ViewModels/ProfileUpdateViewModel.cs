using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace EventManagementSystem.Models.ViewModels
{
    public class ProfileUpdateViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CustomerId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string State { get; set; }

        [StringLength(20)]
        [Display(Name = "Zip Code")]
        public string ZipCode { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Preferred Event Types")]
        public string PreferredEventTypesString { get; set; } = "";

        public List<string> PreferredEventTypes
        {
            get => string.IsNullOrEmpty(PreferredEventTypesString)
                ? new List<string>()
                : PreferredEventTypesString.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            set => PreferredEventTypesString = string.Join(", ", value ?? new List<string>());
        }

        [Display(Name = "Profile Picture")]
        [DataType(DataType.Upload)]
        public IFormFile? ProfilePicture { get; set; }
    }
} 