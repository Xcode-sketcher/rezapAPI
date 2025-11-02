using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace rezapAPI.Model
{
    public class Card
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        [Required]
        public string Icon { get; set; } = string.Empty;
        
        public string? Color { get; set; }
    }
}