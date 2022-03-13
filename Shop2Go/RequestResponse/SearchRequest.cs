using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shop2Go.RequestResponse
{
    public class SearchRequest
    {
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }

        
    }
}
