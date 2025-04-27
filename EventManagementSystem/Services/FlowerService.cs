using System;
using System.Collections.Generic;
using System.Linq;
using EventManagementSystem.Models;

namespace EventManagementSystem.Services
{
    public class FlowerService 
    {
        private readonly ApplicationDbContext _context;

        public FlowerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Flower> GetAllFlowers()
        {
            return _context.Flowers.ToList();
        }

        public Flower GetFlowerById(int id)
        {
            return _context.Flowers.FirstOrDefault(f => f.Id == id);
        }

        public void AddFlower(Flower flower)
        {
            try
            {
                flower.CreatedAt = DateTime.Now;
                flower.UpdatedAt = DateTime.Now;
                
                if (string.IsNullOrEmpty(flower.ImageUrl))
                {
                    flower.ImageUrl = "/images/flowers/default.jpg";
                }
                
                _context.Flowers.Add(flower);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error : {ex.Message}", ex);
            }
        }

        public void UpdateFlower(Flower updatedFlower)
        {
            var existingFlower = _context.Flowers.FirstOrDefault(f => f.Id == updatedFlower.Id);
            if (existingFlower != null)
            {
                existingFlower.Name = updatedFlower.Name;
                existingFlower.Description = updatedFlower.Description;
                existingFlower.Type = updatedFlower.Type;
                existingFlower.Price = updatedFlower.Price;
                existingFlower.Color = updatedFlower.Color;
                existingFlower.IsAvailable = updatedFlower.IsAvailable;
                
                if (!string.IsNullOrEmpty(updatedFlower.ImageUrl))
                {
                    existingFlower.ImageUrl = updatedFlower.ImageUrl;
                }
                
                existingFlower.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
            }
        }
        public void DeleteFlower(int id)
        {
            var flower = _context.Flowers.FirstOrDefault(f => f.Id == id);
            if (flower != null)
            {
                _context.Flowers.Remove(flower);
                _context.SaveChanges();
            }
        }

        public List<Flower> GetFlowersByType(string type)
        {
            return _context.Flowers.Where(f => f.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<Flower> GetAvailableFlowers()
        {
            return _context.Flowers.Where(f => f.IsAvailable).ToList();
        }
    }
} 