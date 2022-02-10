﻿using MasterFood.Models;
using MasterFood.RequestResponse;
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
        public async Task<IActionResult> GetShopByID(string id) 
        {
            Shop shop = this.Service.GetShop(id);
            return Ok(shop);
        }

        [HttpPut]
        [Route("Shop/{id}")]
        public async Task<IActionResult> UpdateShop(string id, [FromBody] ShopRequest changes) 
        {
            //var filter = Builders<Shop>.Filter.Eq("ID", ObjectId.Parse(id));
            //var shop = Shops.Find(filter).First();
            //await Shops.ReplaceOneAsync(filter, updatedS);

            Shop shop = this.Service.GetShop(id);
            if (changes.Name != null)
            {
                shop.Name = changes.Name;
            }
            if (changes.Description != null)
            {
                shop.Description = changes.Description;
            }
            if (changes.Picture != null)
            {
                this.Service.DeleteImage(shop.Picture, IUserService.ImageType.Shop);
                shop.Picture = this.Service.AddImage(changes.Picture, IUserService.ImageType.Shop);
            }
            if (changes.Tags != null)
            {
                shop.Tags = changes.Tags;
            }
            this.Service.UpdateShop(shop);
            return Ok(); 
        }

        [HttpPost]
        [Route("Shop")]
        public async Task<IActionResult> CreateShop([FromForm] ShopCreateRequest data) 
        {

            //create owner
            User user = this.Service.GetUser(null, data.UserName);
            if (user != null)
            {
                return BadRequest(new { message = "User already exists." });
            }
            byte[] password, salt;
            this.Service.CreatePassword(out password, out salt, data.Password);
            user = new User
            {
                UserName = data.UserName,
                Password = password,
                Salt = salt,
                Online = false,
                Shop = null
            };
            this.Service.CreateUser(user);

            //create shop
            User userr = this.Service.GetUser(null, data.UserName);
            if (user.Shop != null)
            {
                return BadRequest(new { message = "Korisnik vec ima prodavnicu." });
            }

            string img_path;
            if (data.Picture != null)
            {
                img_path = this.Service.AddImage(data.Picture, IUserService.ImageType.Shop);
            }
            else
            {
                img_path = "default.png";
            }
            //List<string> tags = null;
            //if (data.Tags != null)
            //{
            //    tags = new List<string>(data.Tags);
            //}
            Shop shop = new Shop
            {
                Name = data.Name,
                Description = data.Description,
                Picture = img_path,
                Tags = data.Tags,
                OrderCount = 0,
                Owner = new MongoDBRef("User", BsonValue.Create(user.ID)),
                Items = null,
                Orders = null
            };

            //store shop


            var userFilter = Builders<User>.Filter.Eq("ID", userr.ID);
            this.Shops.InsertOne(shop);
            user.Shop = new MongoDBRef("Shop", BsonValue.Create(shop.ID));
            //user.Shop = new MongoDBRef("Shop", BsonValue.Create(shop.ID));
            this.Users.ReplaceOne(userFilter, user);
            return Ok();
        }

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

        [HttpPost] // TODO: do we really need ref to shop???
        [Route("Shop/{id}/Item")]
        public async Task<IActionResult> AddItem(string id, [FromForm] ItemRequest newItem)
        {
            string img_path = this.Service.AddImage(newItem.Picture);
            Item item = new Item
            {
                Name = newItem.Name,
                Description = newItem.Description,
                Picture = img_path,
                Price = (double)newItem.Price,
                Amount = 1,
                Shop = new MongoDBRef("Shop", BsonValue.Create(id)),
                Tags = null
            };
            Shop shop = this.Service.GetShop(id);
            if (shop.Items == null)
            {
                shop.Items = new List<Item>();
            }
            else if (shop.Items.Any(x => String.Equals(x.Name, item.Name)))
            {
                return BadRequest(new { message = "Prodavnica vec ima ovaj proizvod." });
            }
            shop.Items.Add(item);
            this.Service.UpdateShop(shop);
            return Ok();
        }

        [HttpPut]
        [Route("Shop/{shopid}/Item/{itemid}")]
        public async Task<IActionResult> UpdateItem(string shopid, string itemid, [FromForm] ItemRequest newItem) 
        {
            //TODO: turn on auth

            //User user = this.Service.GetUser(null, (string)HttpContext.Items["UserName"]);
            //if (user.Shop == null)
            //{
          
                Shop shop = this.Service.GetShop(shopid /*user.Shop.Id.AsString*/);
                if (shop.Items != null && shop.Items.Any(x => String.Equals(x.ID, itemid)))
                {
                    int index = shop.Items.FindIndex(x => String.Equals(x.ID, itemid));
                    if (newItem.Description != null)
                    {
                        shop.Items[index].Description = newItem.Description;
                    }
                    if (newItem.Name != null)
                    {
                        shop.Items[index].Name = newItem.Name;
                    }
                    if (newItem.Price != null)
                    {
                        shop.Items[index].Price = (double)newItem.Price;
                    }
                    if (newItem.Price != null)
                    {
                        this.Service.DeleteImage(shop.Items[index].Picture, IUserService.ImageType.Item);
                        shop.Items[index].Picture = this.Service.AddImage(newItem.Picture, IUserService.ImageType.Item);
                    }
                    if (newItem.Tags != null)
                    {
                        if (shop.Items[index].Tags != null)
                        {
                            shop.Items[index].Tags = new List<string>();
                        }
                        shop.Items[index].Tags = shop.Items[index].Tags.Union(newItem.Tags).ToList();
                    }
                    this.Service.UpdateItem(shop.ID, shop.Items[index]);
                    return Ok();
                }
                else
                {
                    return BadRequest(new { message = "Shop does not have this item." });
                }
            //}
            //else
            //{
            //    return BadRequest(new { message = "User does not have shop." });
            //}
        }
    


        [HttpDelete]
        [Route("Shop/{id}/Item")]
        public async Task<IActionResult> DeleteItem() { return Ok(); }


        #endregion

        #endregion

        #region order methods

        [HttpGet]
        [Route("Shop/{id}/Order")]
        public async Task<IActionResult> GetShopOrders(string id) 
        {

            //var filter = Builders<Order>.Filter.Eq("Shop.ID", ObjectId.Parse(id));
            //var shop = Shops.Find(filter).First();

            return Ok(); 
        }

        [HttpPost]
        [Route("Shop/{id}/Order")]      //TODO: wont accept order items, serialization error
        public async Task<IActionResult> CreateOrder([FromBody] Order newOrder, string id) 
        {
      

            //var filterS = Builders<Shop>.Filter.Eq("ID", ObjectId.Parse(id));
            //var shop = Shops.Find(filterS).First();

            Orders.InsertOne(newOrder);
            newOrder.Shop = new MongoDBRef("Shop", BsonValue.Create(id));

            FilterDefinition<Order> ofilter = Builders<Order>.Filter.Eq(o => o.ID, newOrder.ID);
            Orders.ReplaceOne(ofilter, newOrder );
            return Ok(); 
        }

        [HttpPut]
        [Route("Shop/{id}/Order/OrderID")]
        public async Task<IActionResult> CompleteOrder() { return Ok(); }


        #endregion


    }
}

