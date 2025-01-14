﻿using Shop2Go.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shop2Go.RequestResponse
{
    public class LogInResponse
    {
        public string ID { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }

        public IUserService.AccountType Level { get; set; }
        public string ShopID { get; set; }
    }
}
