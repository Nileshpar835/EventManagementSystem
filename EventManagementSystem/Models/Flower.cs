using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models
{
    public class Flower
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(50, 10000, ErrorMessage = "Price must be greater than 50")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Color is required")]
        public string Color { get; set; }

        public bool IsAvailable { get; set; } = true;

        [Required(ErrorMessage = "Image URL is required")]
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string GetTypeClass()
        {
            return Type.ToLower() switch
            {
                "bouquet" => "bg-pink-100 text-pink-800 dark:bg-pink-700 dark:text-pink-100",
                "centerpiece" => "bg-purple-100 text-purple-800 dark:bg-purple-700 dark:text-purple-100",
                "corsage" => "bg-blue-100 text-blue-800 dark:bg-blue-700 dark:text-blue-100",
                "boutonniere" => "bg-green-100 text-green-800 dark:bg-green-700 dark:text-green-100",
                "wreath" => "bg-yellow-100 text-yellow-800 dark:bg-yellow-700 dark:text-yellow-100",
                _ => "bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-100"
            };
        }

        public string GetColorClass()
        {
            return Color?.ToLower() switch
            {
                "red" => "bg-red-100 text-red-800 dark:bg-red-700 dark:text-red-100",
                "pink" => "bg-pink-100 text-pink-800 dark:bg-pink-700 dark:text-pink-100",
                "white" => "bg-gray-100 text-gray-800 dark:bg-gray-200 dark:text-gray-800",
                "yellow" => "bg-yellow-100 text-yellow-800 dark:bg-yellow-700 dark:text-yellow-100",
                "purple" => "bg-purple-100 text-purple-800 dark:bg-purple-700 dark:text-purple-100",
                "blue" => "bg-blue-100 text-blue-800 dark:bg-blue-700 dark:text-blue-100",
                "orange" => "bg-orange-100 text-orange-800 dark:bg-orange-700 dark:text-orange-100",
                "mixed" => "bg-gradient-to-r from-pink-100 to-purple-100 text-purple-800 dark:from-pink-700 dark:to-purple-700 dark:text-purple-100",
                _ => "bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-100"
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