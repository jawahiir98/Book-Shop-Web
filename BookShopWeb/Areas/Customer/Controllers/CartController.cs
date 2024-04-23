using BookShop.DataAccess.Repository;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookShopWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        public ShoppingCartVM CartVM { get; set; }
        public CartController(IUnitOfWork _unitOfWork)
        {
            unitOfWork = _unitOfWork;
        }
        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50) return shoppingCart.Product.Price;
            else
            {
                if (shoppingCart.Count < 100) return shoppingCart.Product.Price50;
                else return shoppingCart.Product.Price100;
            }
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            CartVM = new()
            {
                ShoppingCartList = unitOfWork.ShoppingCarts.GetAll(
                    u => u.ApplicationUserId == userId,
                    includeProperties: "Product"
                ),
                OrderHeader = new()
            };
            foreach (var cart in CartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                CartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }
            return View(CartVM);
        }
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            CartVM = new()
            {
                ShoppingCartList = unitOfWork.ShoppingCarts.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            CartVM.OrderHeader.ApplicationUser = unitOfWork.ApplicationUsers.Get(u => u.Id == userId);

            CartVM.OrderHeader.Name = CartVM.OrderHeader.ApplicationUser.Name;
            CartVM.OrderHeader.StreetAddress = CartVM.OrderHeader.ApplicationUser.StreetAddress;
            CartVM.OrderHeader.State = CartVM.OrderHeader.ApplicationUser.State;
            CartVM.OrderHeader.City = CartVM.OrderHeader.ApplicationUser.City;
            CartVM.OrderHeader.PostalCode = CartVM.OrderHeader.ApplicationUser.PostalCode;
            CartVM.OrderHeader.PhoneNumber = CartVM.OrderHeader.ApplicationUser.PhoneNumber;

            foreach (var cart in CartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                CartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(CartVM);
        }

        public IActionResult Plus(int CartId)
        {
            var cartFromDb = unitOfWork.ShoppingCarts.Get(u => u.Id ==  CartId);
            cartFromDb.Count += 1;
            unitOfWork.ShoppingCarts.Update(cartFromDb);
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int CartId)
        {
            var cartFromDb = unitOfWork.ShoppingCarts.Get(u => u.Id == CartId);
            if(cartFromDb.Count <= 1)
            {
                unitOfWork.ShoppingCarts.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
            }
            unitOfWork.ShoppingCarts.Update(cartFromDb);
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int CartId)
        {
            var cartFromDb = unitOfWork.ShoppingCarts.Get(u => u.Id == CartId);
            unitOfWork.ShoppingCarts.Remove(cartFromDb);
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
    }
}
