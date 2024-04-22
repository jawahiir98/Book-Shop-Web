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
    public class UnitOfWork : IUnitOfWork
    {

        public ICategoryRepository Categories { get; private set; }
        public ICompanyRepository Companies { get; private set; }
        public IProductRepository Products { get; private set; }
        public IShoppingCartRepository ShoppingCarts { get; private set; }
        public IApplicationUserRepository ApplicationUsers { get; private set; }

        private readonly ApplicationDbContext _db;
        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            ApplicationUsers = new ApplicationUserRepository(_db);
            ShoppingCarts = new ShoppingCartRepository(_db);
            Categories = new CategoryRepository(_db);
            Products = new ProductRepository(_db);
            Companies = new CompanyRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
