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
        IShoppingCartRepository ShoppingCarts { get; }
        ICategoryRepository Categories {get;}
        IProductRepository Products { get;}
        IProductImageRepository ProductImages { get;}
        ICompanyRepository Companies{ get;}
        IOrderHeaderRepository OrderHeaders { get;}
        IOrderDetailRepository OrderDetails { get;}
        
        void Save();
    }
}
