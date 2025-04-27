using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(50, 10000, ErrorMessage = "Price must be greater than 50")]
        public decimal PricePerDay { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        public bool IsAvailable { get; set; } = true;

        [Required(ErrorMessage = "Image URL is required")]
        public string ImageUrl { get; set; } = "/images/equipment/default.jpg";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string GetCategoryClass()
        {
            return Category.ToLower() switch
            {
                "audio" => "bg-blue-100 text-blue-800 ",
                "video" => "bg-purple-100 text-purple-800 ",
                "lighting" => "bg-yellow-100 text-yellow-800 ",
                "decoration" => "bg-green-100 text-green-800 ",
                "furniture" => "bg-red-100 text-red-800 ",
                "electronics" => "bg-indigo-100 text-indigo-800 ",
                _ => "bg-gray-100 text-gray-800"
            };
        }

        public string GetAvailabilityStatusDisplay()
        {
            return IsAvailable 
                ? "text-green-600 dark:text-green-400" 
                : "text-red-600 dark:text-red-400";
        }

        public string GetAvailabilityText()
        {
            return IsAvailable ? "Yes" : "No";
        }

        public string GetAvailabilityIcon()
        {
            return IsAvailable ? "fa-check-circle" : "fa-times-circle";
        }
    }
} 