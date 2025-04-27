using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string? Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        [DataType(DataType.Password)]
        public string? Password { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "User role is required")]
        [Display(Name = "Register as")]
        public UserRole Role { get; set; } = UserRole.Customer;

        [Display(Name = "Organization Name")]
        public string? OrganizationName { get; set; }

        [Display(Name = "Position")]
        public string? Position { get; set; }

        [Display(Name = "Business Phone")]
        [Phone(ErrorMessage = "Invalid business phone number")]
        public string? BusinessPhone { get; set; }

        [Display(Name = "Business Address")]
        public string? BusinessAddress { get; set; }

        [Display(Name = "Business Website")]
        [Url(ErrorMessage = "Invalid website URL")]
        public string? BusinessWebsite { get; set; }

        [Display(Name = "Business Email")]
        [EmailAddress(ErrorMessage = "Invalid business email address")]
        public string? BusinessEmail { get; set; }

        [Display(Name = "Tax ID")]
        public string? TaxId { get; set; }

        [Display(Name = "Venue Specialties")]
        public string? VenueSpecialties { get; set; }
    }
} 