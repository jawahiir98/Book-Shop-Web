using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace BookShopWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        public ProductController(IUnitOfWork _unitOfWork)
        {
            unitOfWork = _unitOfWork;
        }

        public IActionResult Index()
        {
            List<Product> productList = unitOfWork.Products.GetAll().ToList();
            return View(productList);
        }

        public IActionResult Upsert(int? id)
        {
            Product product = new Product();
            if (id == null || id == 0)
            {
                return View(product);
            }
            else
            {
                product = unitOfWork.Products.Get(u => u.Id == id);
                return View(product);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Product product,IFormFile? file)
        {
            if (product.Id == 0)
            {
                unitOfWork.Products.Add(product);
                TempData["success"] = "Product added succesfully";
            }
            else 
            {
                unitOfWork.Products.Update(product);
                TempData["success"] = "Product update succesfully";
            }
            unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult Delete(int? id)
        {
            if (id == 0 || id == null) return NotFound();
            Product product = unitOfWork.Products.Get(u => u.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            if (id == null || id == 0) return NotFound();
            Product prod = unitOfWork.Products.Get(u => u.Id == id);
            if (prod == null) return NotFound();

            unitOfWork.Products.Remove(prod);
            unitOfWork.Save();
            TempData["success"] = "Product deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
