
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Diagnostics;

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
            Product product = unitOfWork.Products.Get(u => u.Id == id, includeProperties: "Category");
            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
