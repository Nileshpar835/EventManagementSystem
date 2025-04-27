using System;
using System.Collections.Generic;
using System.Linq;
using EventManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Services
{
    public class FoodService
    {
        private readonly ApplicationDbContext _context;

        public FoodService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Food> GetAllFoods()
        {
            return _context.Foods.ToList();
        }

        public Food GetFoodById(int id)
        {
            return _context.Foods.FirstOrDefault(f => f.Id == id);
        }

        public void AddFood(Food food)
        {
            if (string.IsNullOrEmpty(food.Description))
                food.Description = "";
            
            if (string.IsNullOrEmpty(food.DietaryInfo))
                food.DietaryInfo = "";
            
            if (string.IsNullOrEmpty(food.ImageUrl))
                food.ImageUrl = "/images/placeholder-food.jpg";
            
            food.CreatedAt = DateTime.Now;
            food.UpdatedAt = DateTime.Now;
            
            _context.Foods.Add(food);
            _context.SaveChanges();
        }

        public void UpdateFood(Food updatedFood)
        {
            var existingFood = _context.Foods.FirstOrDefault(f => f.Id == updatedFood.Id);
            if (existingFood != null)
            {
                existingFood.Name = updatedFood.Name;
                existingFood.Description = string.IsNullOrEmpty(updatedFood.Description) ? "" : updatedFood.Description;
                existingFood.Category = updatedFood.Category;
                existingFood.PricePerPerson = updatedFood.PricePerPerson;
                existingFood.DietaryInfo = string.IsNullOrEmpty(updatedFood.DietaryInfo) ? "" : updatedFood.DietaryInfo;
                existingFood.IsAvailable = updatedFood.IsAvailable;
                
                if (!string.IsNullOrEmpty(updatedFood.ImageUrl))
                {
                    existingFood.ImageUrl = updatedFood.ImageUrl;
                }
                
                existingFood.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public void DeleteFood(int id)
        {
            var food = _context.Foods.FirstOrDefault(f => f.Id == id);
            if (food != null)
            {
                _context.Foods.Remove(food);
                _context.SaveChanges();
            }
        }

        public List<Food> GetFoodsByCategory(string category)
        {
            return _context.Foods
                .Where(f => f.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<Food> GetAvailableFoods()
        {
            return _context.Foods.Where(f => f.IsAvailable).ToList();
        }
    }
} 