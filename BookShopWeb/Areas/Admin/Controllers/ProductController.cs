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
        private readonly IWebHostEnvironment webHostEnvironment;
        public ProductController(IUnitOfWork _unitOfWork, IWebHostEnvironment _webHostEnvironment)
        {
            unitOfWork = _unitOfWork;
            webHostEnvironment = _webHostEnvironment;

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
            if (ModelState.IsValid)
            {
                string wwwRootPath = webHostEnvironment.WebRootPath;
                if(file != null)
                {
                    string filename = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    // Delete old image if exits in editing image.
                    if (!string.IsNullOrEmpty(productvm.Product.ImageUrl))
                    {
                        var oldPath = Path.Combine(wwwRootPath, productvm.Product.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }
                    using (var fileStream = new FileStream(Path.Combine(productPath, filename), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    productvm.Product.ImageUrl = @"\images\product\" + filename;
                    
                }
                if (productvm.Product.Id == 0)
                {
                    TempData["success"] = "Product created successfully";
                    unitOfWork.Products.Add(productvm.Product);
                }
                else
                {
                    TempData["success"] = "Product edited successfully";
                    unitOfWork.Products.Update(productvm.Product);
                }

                unitOfWork.Save();
                
                return RedirectToAction("Index");

            }
            else
            {
                productvm.CategoryList = unitOfWork.Categories.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productvm);
            }
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
