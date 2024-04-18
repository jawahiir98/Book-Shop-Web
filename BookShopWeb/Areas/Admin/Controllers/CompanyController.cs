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
    //[Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IWebHostEnvironment webHostEnvironment;
        public CompanyController(IUnitOfWork _unitOfWork, IWebHostEnvironment _webHostEnvironment)
        {
            unitOfWork = _unitOfWork;
            webHostEnvironment = _webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Company> companyList = unitOfWork.Companies.GetAll().ToList();
            
            return View(companyList);
        }

        public IActionResult Upsert(int? id)
        {
            Company company = new Company();
            if (id == null || id == 0)
            {
                return View(new Company());
            }
            else
            {
                company = unitOfWork.Companies.Get(u => u.Id == id);
                return View(company);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company companyObj)
         {
            if (ModelState.IsValid)
            {
                if (companyObj.Id == 0)
                {
                    TempData["success"] = "Company created successfully";
                    unitOfWork.Companies.Add(companyObj);
                }
                else
                {
                    TempData["success"] = "Company edited successfully";
                    unitOfWork.Companies.Update(companyObj);
                }

                unitOfWork.Save();
                
                return RedirectToAction("Index");

            }
            else
            {
                return View(companyObj);
            }
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> companyList = unitOfWork.Companies.GetAll().ToList();
            return Json(new {data = companyList});
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = unitOfWork.Companies.Get(u => u.Id == id);
            if (obj == null) return Json(new { success = false, message ="Error while deleting." });

            unitOfWork.Companies.Remove(obj);
            unitOfWork.Save();
            return Json(new { success = true, message = "Successfully deleted." });
        }
        #endregion
    }
}
