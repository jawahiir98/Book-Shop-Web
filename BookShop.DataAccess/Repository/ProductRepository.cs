using BookShop.DataAccess.Data;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShop.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
        public void Update(Product product)
        {
            var obj = _db.Products.FirstOrDefault(u => u.Id == product.Id);
            if(obj != null)
            {
                obj.Title = product.Title;
                obj.Description = product.Description;
                obj.Author = product.Author;
                obj.ISBN = product.ISBN;
                obj.ListPrice = product.ListPrice;
                obj.Price = product.Price;
                obj.Price50 = product.Price50;
                obj.Price100 = product.Price100;
                obj.Category = product.Category;
                obj.CategoryId = product.CategoryId;
                //if(obj.ImageUrl != null)
                //{
                //    obj.ImageUrl = product.ImageUrl;
                //}
            }
        }
    }
}
