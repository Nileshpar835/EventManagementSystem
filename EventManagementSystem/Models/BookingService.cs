using System;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models
{
    public class BookingService
    {
        public int Id { get; set; }
        
        public int BookingId { get; set; }
        
        public string ServiceType { get; set; } 
        
        public int ServiceId { get; set; }
        
        public string ServiceName { get; set; }
        
        public decimal ServicePrice { get; set; }
        
        public int Quantity { get; set; }
        
        public decimal TotalPrice => ServicePrice * Quantity;
        
        public string ImageUrl { get; set; }
    }
} 