using MasterFood.Models;
using MasterFood.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasterFood.Controllers
{
    public class ShopController : Controller
    {
        public IUserService Service { get; set; }
       // public IWebHostEnvironment Environment { get; set; }
        public AppSettings _appSettings { get; set; }

        private readonly IMongoCollection<Shop> Shops;
        private readonly IMongoCollection<Order> Orders;
        private readonly IMongoCollection<User> Users;
        private readonly IMongoDatabase database;

        public ShopController(IUserService service/*, IWebHostEnvironment environment*/, IOptions<DbSettings> dbSettings, IOptions<AppSettings> appsettings)
        {
           
            //this.Environment = environment;
            this._appSettings = appsettings.Value;
            MongoClient client = new MongoClient(dbSettings.Value.ConnectionString);
            database = client.GetDatabase(dbSettings.Value.DatabaseName);
            this.Shops = database.GetCollection<Shop>(dbSettings.Value.ShopCollectionName);
            this.Orders = database.GetCollection<Order>(dbSettings.Value.OrderCollectionName);
            this.Users = database.GetCollection<User>(dbSettings.Value.UserCollectionName);
            this.Service = service;
        }

        #region Shop methods

        [HttpGet]
        [Route("Shop")] //return all shops
        public async Task<IActionResult> GetShops() 
        {
            var shops = await  Shops.Find(new BsonDocument()).ToListAsync();
            return Ok(shops);

        }

        [HttpGet]
        [Route("Shop/Popular")] //return popular 6
        public async Task<IActionResult> GetPopularShops() { return Ok(); }

        [HttpGet]
        [Route("Shop/{id}")]
        public async Task<IActionResult> GetShopByID() { return Ok(); }

        [HttpPut]
        [Route("Shop/{id}")]
        public async Task<IActionResult> UpdateShop() { return Ok(); }

        [HttpPost]
        [Route("Shop")]
        public async Task<IActionResult> CreateShop() { return Ok(); }

        [HttpDelete]
        [Route("Shop/{id}")]
        public async Task<IActionResult> DeleteShop(string id)
        {
            string username = (string)HttpContext.Items["UserName"];
            User user = this.Service.GetUser(null, username);

            var filterS = Builders<Shop>.Filter.Eq("ID", ObjectId.Parse(id));
            var shop =  Shops.Find(filterS).First();

            var owner = Users.Find<User>(u => u.Shop.Id == shop.ID).First();
            var filterU = Builders<User>.Filter.Eq("ID", owner.ID);

            Shops.DeleteOne(filterS);
            Users.DeleteOne(filterU);
            return Ok();
        }

        [HttpGet]
        [Route("Shop/Tags")]
        public async Task<IActionResult> GetShopTags() 
        {
            return Ok();
        }
        #region shop item methods

        //[HttpPost]
        //[Route("Shop/{id}/Item")]
        //public async Task<IActionResult> AddItem() 
        //{
        //    var filter = Builders<T>.Filter.Eq("ID", id);
        //    return Item.Find(filter).First();
        //}

        [HttpPut]
        [Route("Shop/{id}/Item")]
        public async Task<IActionResult> UpdateItem() { return Ok(); }


        [HttpDelete]
        [Route("Shop/{id}/Item")]
        public async Task<IActionResult> DeleteItem() { return Ok(); }


        #endregion

        #endregion

        #region order methods

        [HttpGet]
        [Route("Shop/{id}/Order")]
        public async Task<IActionResult> GetShopOrders() { return Ok(); }

        [HttpPost]
        [Route("Shop/{id}/Order")]
        public async Task<IActionResult> CreateOrder() { return Ok(); }

        [HttpPut]
        [Route("Shop/{id}/Order/OrderID")]
        public async Task<IActionResult> CompleteOrder() { return Ok(); }


        #endregion


    }
}

