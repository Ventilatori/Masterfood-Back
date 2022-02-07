using MasterFood.Authentication;
using MasterFood.Models;
using MasterFood.RequestResponse;
using MasterFood.Service;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
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

        [HttpGet]
        [Route("GetAllShops/{page_num}/{page_size}")]
        public async Task<IActionResult> GetShops(int page_num, int page_size)
        {
            List<Shop> shops = this.Service.GetAllShops(page_num,page_size);
            return Ok(shops);
        }

        [HttpGet]
        [Route("GetPopularShops/{page_size}")]
        public async Task<IActionResult> GetPopularShops(int page_size)
        {
            List<Shop> shops = this.Service.PopularShops(page_size);
            return Ok(shops);
        }

        [HttpGet]
        [Route("GetShop/{id}")]
        public async Task<IActionResult> GetShop(string id)
        {
            Shop shop = this.Service.GetShop(id);
            return Ok(shop);
        }

        [Auth]
        [HttpPut]
        [Route("UpdateShop/{id}")]
        public async Task<IActionResult> UpdateShop(string id, [FromBody] ShopRequest changes)
        {
            Shop shop = this.Service.GetShop(id);
            if (changes.Name != null)
            {
                shop.Name = changes.Name;
            }
            if(changes.Description != null)
            {
                shop.Description = changes.Description;
            }
            if (changes.Picture != null)
            {
                if (!String.Equals(shop.Picture, "default"))
                {
                    this.Service.DeleteImage(shop.Picture, IUserService.ImageType.Shop);
                }
                shop.Picture = this.Service.AddImage(changes.Picture, IUserService.ImageType.Shop);
            }
            if (changes.Tags != null)
            {
                shop.Tags = shop.Tags.Union(changes.Tags).ToList();
            }
            this.Service.UpdateShop(shop);
            return Ok();
        }

        [Auth("Admin")]
        [HttpPost]
        [Route("CreateShop/{userID}")]
        public async Task<IActionResult> CreateShop(string userID, [FromBody] ShopRequest newShop)
        {
            User user = this.Service.GetUser(userID);

            string img_path;
            if (newShop.Picture != null)
            {
                img_path = this.Service.AddImage(newShop.Picture, IUserService.ImageType.Shop);
            }
            else
            {
                img_path = "default.png";
            }
            List<string> tags = null;
            if (newShop.Tags!=null)
            {
                tags = new List<string>(newShop.Tags);
            }
            Shop shop = new Shop
            {
                Name = newShop.Name,
                Description = newShop.Description,
                Picture = img_path,
                Tags = tags,
                OrderCount = 0,
                Owner = new MongoDBRef("User", userID),
                Items = null,
                Orders = null
            };
            this.Service.StoreShop(shop, user);
            return Ok();
        }

        [Auth("Admin")]
        [HttpPost]
        [Route("PostItem/{id}")]
        public async Task<IActionResult> PostItem(string id, [FromBody] NewItem newItem)
        {
            string img_path = this.Service.AddImage(newItem.Picture);
            Item item = new Item
            {
                Name = newItem.Name,
                Description = newItem.Description,
                Picture = img_path,
                Price = newItem.Price,
                Amount = 1,
                Shop = new MongoDBRef("Shop", id),
                Tags = null
            };
            Shop shop = this.Service.GetShop(id);
            if (shop.Items != null)
            {
                shop.Items = new List<Item>();
            }
            shop.Items.Add(item);
            this.Service.UpdateShop(shop);
            return Ok();
        }

        


    }
}
