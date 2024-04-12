using BookShop.DataAccess.Repository;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            ProductVM pvm = new()
            {
                CategoryList = unitOfWork.Categories.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                return View(pvm);
            }
            else
            {
                pvm.Product = unitOfWork.Products.Get(u => u.Id == id);
                return View(pvm);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productvm,IFormFile? file)
        {
            if (productvm.Product.Id == 0)
            {
                unitOfWork.Products.Add(productvm.Product);
                TempData["success"] = "Product added succesfully";
            }
            else 
            {
                unitOfWork.Products.Update(productvm.Product);
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
