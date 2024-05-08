
using BookShop.DataAccess.Repository;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Diagnostics;
using System.Security.Claims;

namespace BookShopWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly IUnitOfWork unitOfWork;
        public HomeController(ILogger<HomeController> _logger, IUnitOfWork _unitOfWork)
        {
            logger = _logger;
            unitOfWork = _unitOfWork;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> productList = unitOfWork.Products.GetAll(includeProperties: "Category,ProductImages").ToList();
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            ShoppingCart cart = new ShoppingCart()
            {
                Product = unitOfWork.Products.Get(u => u.Id == id, includeProperties: "Category,ProductImages"),
                Count = 1,
                ProductId = id
            };
            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            //cart.Id = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            cart.ApplicationUserId = userId;

            ShoppingCart shoppingCart = unitOfWork.ShoppingCarts.Get(
                u => u.ApplicationUserId == cart.ApplicationUserId &&
                u.ProductId == cart.ProductId
            );
            if(shoppingCart != null)
            {
                // Cart already exists for the product..So we just update the existing one.
                shoppingCart.Count += cart.Count;
                unitOfWork.ShoppingCarts.Update(shoppingCart);
                unitOfWork.Save();
            }
            else
            {
                unitOfWork.ShoppingCarts.Add(cart);
                unitOfWork.Save();
                HttpContext.Session.SetInt32(
                    SD.SessionCart, 
                    unitOfWork.ShoppingCarts.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).Count()
                );
            }

            TempData["success"] = "Cart added successfully.";
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Privacy()
        {
            return View();
        }
    }
}
