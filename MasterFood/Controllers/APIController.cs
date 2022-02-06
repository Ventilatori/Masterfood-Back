using MasterFood.Service;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasterFood.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class APIController : Controller
    {
        public IUserService Service { get; set; }

        public APIController(IUserService service)
        {
            this.Service = service;
        }





    }
}
