using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models
{
    public class Food
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string? Description { get; set; } = "";

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(50, 10000, ErrorMessage = "Price must be greater than 50")]
        public decimal PricePerPerson { get; set; }

        public string? DietaryInfo { get; set; } = "";

        public bool IsAvailable { get; set; } = true;

        public string? ImageUrl { get; set; } = "/images/placeholder-food.jpg";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string GetCategoryClass()
        {
            return Category?.ToLower() switch
            {
                "appetizer" => "bg-yellow-100 text-yellow-800 dark:bg-yellow-700 dark:text-yellow-100",
                "main course" => "bg-blue-100 text-blue-800 dark:bg-blue-700 dark:text-blue-100",
                "dessert" => "bg-purple-100 text-purple-800 dark:bg-purple-700 dark:text-purple-100",
                "beverage" => "bg-green-100 text-green-800 dark:bg-green-700 dark:text-green-100",
                "side dish" => "bg-red-100 text-red-800 dark:bg-red-700 dark:text-red-100",
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