using System;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models
{
    public class Light
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Range(50, 10000)]
        [Display(Name = "Price Per Day")]
        public decimal PricePerDay { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        [StringLength(50)]
        public string Color { get; set; }

        [Range(0, 10000)]
        [Display(Name = "Power (Watts)")]
        public int Power { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string GetTypeClass()
        {
            return Type.ToLower() switch
            {
                "spotlight" => "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300",
                "led strip" => "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300",
                "laser" => "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300",
                "ambient" => "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300",
                "disco" => "bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-300",
                _ => "bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300"
            };
        }

        public string GetAvailabilityText()
        {
            if (!IsActive)
                return "Unavailable";
            
            return Quantity > 0 ? "Available" : "Out of Stock";
        }

        public string GetAvailabilityIcon()
        {
            if (!IsActive)
                return "fa-times-circle";
            
            return Quantity > 0 ? "fa-check-circle" : "fa-exclamation-circle";
        }

        public string GetAvailabilityStatusDisplay()
        {
            if (!IsActive)
                return "text-gray-500 dark:text-gray-400";
            
            return Quantity > 0 ? "text-green-500 dark:text-green-400" : "text-red-500 dark:text-red-400";
        }
    }
} 