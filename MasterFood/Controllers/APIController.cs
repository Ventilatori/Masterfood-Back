using MasterFood.Authentication;
using MasterFood.Models;
using MasterFood.RequestResponse;
using MasterFood.Service;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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
        [HttpGet]
        [Route("GetMyShop")]
        public async Task<IActionResult> GetMyShop()
        {
            string username = (string)HttpContext.Items["UserName"];
            User user = this.Service.GetUser(null, username);
            if (user.Shop != null)
            {
                var vrsta = user.Shop.Id.GetType();
                Shop shop = this.Service.GetShop(user.Shop.Id.AsString);
                return Ok(user.Shop);
            }
            else
            {
                return BadRequest(new { message = "Korisnik nema svoju prodavnicu. Napravite je."});
            }
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
                this.Service.DeleteImage(shop.Picture, IUserService.ImageType.Shop);
                shop.Picture = this.Service.AddImage(changes.Picture, IUserService.ImageType.Shop);
            }
            if (changes.Tags != null)
            {
                //shop.Tags = shop.Tags.Union(changes.Tags).ToList();  ADAPTIRAJ JE SU SAD TAGOVI STRINGS
            }
            this.Service.UpdateShop(shop);
            return Ok();
        }

        [Auth]
        [HttpPost]
        [Route("CreateShop")]
        public async Task<IActionResult> CreateShop([FromBody] ShopRequest newShop)
        {
            User user = this.Service.GetUser(null, (string)HttpContext.Items["UserName"]);
            if (user.Shop != null)
            {
                return BadRequest(new { message = "Korisnik vec ima prodavnicu."});
            }

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
                //Tags = tags, ADAPTIRAJ JER SU SADA TAGOVI STRING
                OrderCount = 0,
                Owner = new MongoDBRef("User", BsonValue.Create(user.ID)),
                Items = null,
                Orders = null
            };
            this.Service.StoreShop(shop, user);
            return Ok();
        }

        [Auth]
        [HttpPost]
        [Route("PostItem/{id}")]
        public async Task<IActionResult> PostItem(string id, [FromBody] ItemRequest newItem)
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
            if (shop.Items != null)
            {
                shop.Items = new List<Item>();
            }
            else if(shop.Items.Any(x => String.Equals(x.Name, item.Name)))
            {
                return BadRequest(new { message = "Prodavnica vec ima ovaj proizvod." });
            }
            shop.Items.Add(item);
            this.Service.UpdateShop(shop);
            return Ok();
        }

        [Auth]
        [HttpPut]
        [Route("ChangeItem")]
        public async Task<IActionResult> ChangeItem([FromBody] ItemRequest newItem)
        {
            User user = this.Service.GetUser(null, (string)HttpContext.Items["UserName"]);
            if (user.Shop != null)
            {
                Shop shop = this.Service.GetShop(user.Shop.Id.AsString);
                if (shop.Items != null && shop.Items.Any(x => String.Equals(x.Name, newItem.Name)))
                {
                    int index = shop.Items.FindIndex(x => String.Equals(x.Name, newItem.Name));
                    if (newItem.Description != null)
                    {
                        shop.Items[index].Description = newItem.Description;
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
                    return BadRequest(new { message = "Prodavnica nema ovaj proizvod." });
                }
            }
            else
            {
                return BadRequest(new { message = "Korisnik nema prodavnicu." });
            }
        }

    }
}
