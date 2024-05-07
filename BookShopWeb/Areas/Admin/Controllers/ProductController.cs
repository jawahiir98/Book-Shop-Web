using BookShop.DataAccess.Repository;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Models.ViewModels;
using BookShop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Reflection;

namespace BookShopWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
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
            List<Product> productList = unitOfWork.Products.GetAll(includeProperties:"Category").ToList();
            
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
        public IActionResult Upsert(ProductVM productvm,List<IFormFile>? files)
         {
            if (ModelState.IsValid)
            {
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

                string wwwRootPath = webHostEnvironment.WebRootPath;
                if(files != null)
                {
                    bool imageAdded = false;
                    foreach(IFormFile file in  files) {
                        string filename = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\products\product-"+ productvm.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath)) Directory.CreateDirectory(finalPath);

                        using (var fileStream = new FileStream(Path.Combine(finalPath, filename), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + filename,
                            ProductId = productvm.Product.Id,
                        };

                        if(productvm.Product.ProductImages == null)
                        {
                            productvm.Product.ProductImages = new List<ProductImage>();
                        }
                        else
                        {
                            imageAdded = true;
                        }

                        productvm.Product.ProductImages.Add(productImage);
                    }

                    unitOfWork.Products.Update(productvm.Product);
                    unitOfWork.Save();
                    
                    if(imageAdded) { TempData["success"] += "Image Added successfully."; }
                }
                
                return RedirectToAction("Index");

            }
            else
            {
                productvm.CategoryList = unitOfWork.Categories.GetAll(includeProperties: "Category").Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productvm);
            }
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> productList = unitOfWork.Products.GetAll(includeProperties: "Category").ToList();
            return Json(new {data = productList});
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = unitOfWork.Products.Get(u => u.Id == id);
            if (obj == null) return Json(new { success = false, message ="Error while deleting." });

            string wwwRootPath = webHostEnvironment.WebRootPath;
            //string oldImagePath = Path.Combine(wwwRootPath, obj.ImageUrl.TrimStart('\\'));

            //if (System.IO.File.Exists(oldImagePath))
            //{
            //    System.IO.File.Delete(oldImagePath);
            //}
            unitOfWork.Products.Remove(obj);
            unitOfWork.Save();
            return Json(new { success = true, message = "Successfully deleted." });
        }
        #endregion
    }
}
