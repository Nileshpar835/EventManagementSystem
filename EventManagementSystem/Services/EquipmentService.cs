using System;
using System.Collections.Generic;
using System.Linq;
using EventManagementSystem.Models;

namespace EventManagementSystem.Services
{
    public class EquipmentService
    {
        private readonly ApplicationDbContext _context;

        public EquipmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Equipment> GetAllEquipment()
        {
            return _context.Equipment.ToList();
        }

        public Equipment GetEquipmentById(int id)
        {
            return _context.Equipment.FirstOrDefault(e => e.Id == id);
        }

        public void AddEquipment(Equipment equipment)
        {
            equipment.CreatedAt = DateTime.Now;
            equipment.UpdatedAt = DateTime.Now;
            _context.Equipment.Add(equipment);
            _context.SaveChanges();
        }

        public void UpdateEquipment(Equipment updatedEquipment)
        {
            var existingEquipment = _context.Equipment.FirstOrDefault(e => e.Id == updatedEquipment.Id);
            if (existingEquipment != null)
            {
                existingEquipment.Name = updatedEquipment.Name;
                existingEquipment.Description = updatedEquipment.Description;
                existingEquipment.Category = updatedEquipment.Category;
                existingEquipment.PricePerDay = updatedEquipment.PricePerDay;
                existingEquipment.Quantity = updatedEquipment.Quantity;
                existingEquipment.IsAvailable = updatedEquipment.IsAvailable;
                
                if (!string.IsNullOrEmpty(updatedEquipment.ImageUrl))
                {
                    existingEquipment.ImageUrl = updatedEquipment.ImageUrl;
                }
                
                existingEquipment.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public void DeleteEquipment(int id)
        {
            var equipment = _context.Equipment.FirstOrDefault(e => e.Id == id);
            if (equipment != null)
            {
                _context.Equipment.Remove(equipment);
                _context.SaveChanges();
            }
        }

        public List<Equipment> GetEquipmentByCategory(string category)
        {
            return _context.Equipment.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<Equipment> GetAvailableEquipment()
        {
            return _context.Equipment.Where(e => e.IsAvailable && e.Quantity > 0).ToList();
        }
    }
} 