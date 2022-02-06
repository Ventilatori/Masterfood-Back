using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MasterFood.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace MasterFood.Service
{
    public interface IUserService
    {
        enum OrderStatus
        {
            Waiting,
            OnTheWay,
            Done
        };
        enum ImageType
        {
            Shop,
            Item
        };
        enum AccountType
        {
            Shop,
            Admin
        };
        string AddImage(IFormFile? image, ImageType img_type);
        bool DeleteImage(string image, IUserService.ImageType img_type);
        string GenerateToken(User user);
        (string username, IUserService.AccountType type)? CheckToken(string token);
        User FindUser(string id);
        /*
        int PinGenerator();
        void PinUpdate(Korisnik korisnik, int PIN);
        bool ProveriSifru(byte[] sifra, byte[] salt, string zahtev);
        void HesirajSifru(out byte[] hash, out byte[] salt, string sifra);
        string? ProveriPrisutpNalogu(int id, Korisnik tmpKorisnik = null);
        */
    }

    public class UserService : IUserService
    {
        public IWebHostEnvironment Environment { get; set; }
        public AppSettings _appSettings { get; set; }

        public readonly IMongoCollection<Shop> Shops;
        public readonly IMongoCollection<Order> Orders;
        public readonly IMongoCollection<User> Users;

        public UserService(IWebHostEnvironment environment, IOptions<DbSettings> dbSettings, IOptions<AppSettings> appsettings) {
            this.Environment = environment;
            this._appSettings = appsettings.Value;
            MongoClient client = new MongoClient(dbSettings.Value.ConnectionString);
            IMongoDatabase database = client.GetDatabase(dbSettings.Value.DatabaseName);
            this.Shops = database.GetCollection<Shop>(dbSettings.Value.ShopCollectionName);
            this.Orders = database.GetCollection<Order>(dbSettings.Value.OrderCollectionName);
            this.Users = database.GetCollection<User>(dbSettings.Value.UserCollectionName);
        }
        public string AddImage(IFormFile? image, IUserService.ImageType img_type)
        {
            string folderPath = "Images\\"+ img_type.ToString();
            string uploadsFolder = Path.Combine(Environment.WebRootPath, folderPath);
            string file_name;
            if (image != null)
            {
                file_name = Guid.NewGuid().ToString() + "_" + image.FileName;
                string filePath = Path.Combine(uploadsFolder, file_name);
                image.CopyTo(new FileStream(filePath, FileMode.Create));
            }
            else
            {
                file_name = "default.png";
            }
            return file_name;
        }
        public bool DeleteImage(string image, IUserService.ImageType img_type)
        {
            if(!String.Equals(image, "default.png"))
            {
                string folderPath = "Images\\" + img_type.ToString();
                string uploadsFolder = Path.Combine(Environment.WebRootPath, folderPath);
                string filePath = Path.Combine(uploadsFolder, image);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        public string GenerateToken(User user)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.ID) }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public (string username, IUserService.AccountType type)? CheckToken(string token)
        {
            if (!String.IsNullOrEmpty(token))
            {
                (string username, IUserService.AccountType type) result;

                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                byte[] key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.FromMinutes(10)
                }, out SecurityToken validatedToken);

                JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;
                //int userID = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                string userID = jwtToken.Claims.First(x => x.Type == "id").Value;

                User user = this.FindUser(userID);
                result.username = user.UserName;
                result.type = user.UserType;

                return result;
            }
            else
            {
                return null;
            }
        }
        public User FindUser(string id)
        {
            return this.Users.Find(u => u.ID == id).FirstOrDefaultAsync().Result;
        }

        /*
        public bool ProveriSifru(byte[] sifra, byte[] salt, string zahtev)
        {
            HMACSHA512 hashObj = new HMACSHA512(salt);
            byte[] password = System.Text.Encoding.UTF8.GetBytes(zahtev);
            byte[] hash = hashObj.ComputeHash(password);

            int len = hash.Length;
            for (int i = 0; i < len; i++)
            {
                if (sifra[i] != hash[i])
                {
                    return false;
                }
            }
            return true;
        }
        public void HesirajSifru(out byte[] hash, out byte[] salt, string sifra)
        {
            HMACSHA512 hashObj = new HMACSHA512();
            salt = hashObj.Key;
            byte[] password = Encoding.UTF8.GetBytes(sifra);
            hash = hashObj.ComputeHash(password);
        }
        public int PinGenerator()
        {
            int _min = 1000;
            int _max = 9999;
            Random _rdm = new Random();
            return _rdm.Next(_min, _max);
        }
        public void PinUpdate(Korisnik korisnik, int PIN) {
            korisnik.PIN = PIN;
            Context.Update<Korisnik>(korisnik);
            Context.SaveChanges();
        }
        public string? ProveriPrisutpNalogu(int id, Korisnik tmpKorisnik = null)
        {
            Korisnik korisnik = Context.Korisnici.Find(id);
            if (korisnik == null)
            {
                string message = "Korisnik ne postoji. Moguce da je poslat pogresan ID.";
                return message;
            }
            else if (korisnik.Odobren == false)
            {
                string message = "Nalog nije odobren.";
                return message;
            }
            if(tmpKorisnik != null)
            {
                if (id != tmpKorisnik.ID)
                {
                    string message = "Nemate pristup ovom nalogu.";
                    return message;
                }
            }
            return null;
        }
        */
    }
}
