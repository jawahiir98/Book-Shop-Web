
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
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
            IEnumerable<Product> productList = unitOfWork.Products.GetAll(includeProperties: "Category").ToList();
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            ShoppingCart cart = new() {
                Product = unitOfWork.Products.Get(u => u.Id == id, includeProperties: "Category"),
                Count = 1,
                ProductId = id
            };
            return View(cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            cart.ApplicationUserId = userId;
            unitOfWork.ShoppingCarts.Add(cart);
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
