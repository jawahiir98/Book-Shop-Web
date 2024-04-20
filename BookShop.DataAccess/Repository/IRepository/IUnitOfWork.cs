using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShop.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        IApplicationUserRepository ApplicationUsers { get; }
        ICategoryRepository Categories {get;}
        IProductRepository Products { get;}
        ICompanyRepository Companies{ get;}
        IShoppingCartRepository ShoppingCarts { get;}
        void Save();
    }
}
