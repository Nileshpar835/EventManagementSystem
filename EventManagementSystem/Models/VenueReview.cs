using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Models
{
    public class VenueReviewModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int VenueId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string UserName { get; set; }
        
        public string UserImage { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        
        public string Comment { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [ForeignKey("VenueId")]
        public virtual Venue Venue { get; set; }
    }
} 