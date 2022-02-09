using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MasterFood.RequestResponse
{
    public class ItemRequest
    {
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public IFormFile? Picture { get; set; }
        public List<string>? Tags { get; set; }
    }
}
