using System;
using System.Collections.Generic;
using System.Linq;
using EventManagementSystem.Models;

namespace EventManagementSystem.Services
{
    public class LightService
    {
        private readonly ApplicationDbContext _context;

        public LightService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Light> GetAllLights()
        {
            return _context.Lights.ToList();
        }

        public Light GetLightById(int id)
        {
            return _context.Lights.FirstOrDefault(l => l.Id == id);
        }

        public void AddLight(Light light)
        {
            light.CreatedAt = DateTime.Now;
            light.UpdatedAt = DateTime.Now;
            _context.Lights.Add(light);
            _context.SaveChanges();
        }

        public void UpdateLight(Light updatedLight)
        {
            var existingLight = _context.Lights.FirstOrDefault(l => l.Id == updatedLight.Id);
            if (existingLight != null)
            {
                existingLight.Name = updatedLight.Name;
                existingLight.Description = updatedLight.Description;
                existingLight.Type = updatedLight.Type;
                existingLight.PricePerDay = updatedLight.PricePerDay;
                existingLight.Quantity = updatedLight.Quantity;
                existingLight.Color = updatedLight.Color;
                existingLight.Power = updatedLight.Power;
                existingLight.IsActive = updatedLight.IsActive;
                
                if (!string.IsNullOrEmpty(updatedLight.ImageUrl))
                {
                    existingLight.ImageUrl = updatedLight.ImageUrl;
                }
                
                existingLight.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public void DeleteLight(int id)
        {
            var light = _context.Lights.FirstOrDefault(l => l.Id == id);
            if (light != null)
            {
                _context.Lights.Remove(light);
                _context.SaveChanges();
            }
        }

        public List<Light> GetAvailableLights()
        {
            return _context.Lights.Where(l => l.IsActive).ToList();
        }
    }
} 