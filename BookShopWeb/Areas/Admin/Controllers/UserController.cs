using BookShop.DataAccess.Data;
using BookShop.DataAccess.Repository;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Models.ViewModels;
using BookShop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace BookShopWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller 
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        public UserController(IUnitOfWork _unitOfWork,UserManager<IdentityUser> _userManager,RoleManager<IdentityRole> _roleManager)
        {
            unitOfWork = _unitOfWork;
            userManager = _userManager;
            roleManager = _roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult RoleManagement(string userId)
        {
            RoleManagementVM roleVM = new RoleManagementVM() {
                ApplicationUser = unitOfWork.ApplicationUsers.Get(u => u.Id == userId,includeProperties:"Company"),
                RoleList = roleManager.Roles.Select(i=> new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = unitOfWork.Companies.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };
            roleVM.ApplicationUser.Role = userManager.GetRolesAsync(unitOfWork.ApplicationUsers.Get(u => u.Id == userId)).
                GetAwaiter().GetResult().FirstOrDefault();
            return View(roleVM);
        }

        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
        {
            var oldRole = userManager.GetRolesAsync(unitOfWork.ApplicationUsers.Get(u => u.Id == roleManagementVM.ApplicationUser.Id)).
                GetAwaiter().GetResult().FirstOrDefault();
            ApplicationUser applicationUser = unitOfWork.ApplicationUsers.Get(u => u.Id == roleManagementVM.ApplicationUser.Id);

            if (roleManagementVM.ApplicationUser.Role != oldRole)
            {
                // Role has been updated. If updated to company then update the company
                if(roleManagementVM.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                }
                if(oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                unitOfWork.ApplicationUsers.Update(applicationUser);
                unitOfWork.Save();

                userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                userManager.AddToRoleAsync(applicationUser, roleManagementVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else
            {
                if(oldRole == SD.Role_Company && applicationUser.CompanyId != roleManagementVM.ApplicationUser.CompanyId)
                {
                    applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                    unitOfWork.ApplicationUsers.Update(applicationUser);
                    unitOfWork.Save();
                }
            }
            return RedirectToAction("Index");
        }


        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> userList = unitOfWork.ApplicationUsers.GetAll(includeProperties: "Company").ToList();

            foreach(var user in userList)
            {
                user.Role = userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

                if(user.Company == null)
                {
                    user.Company = new() { Name = "" };
                }
            }
            return Json(new {data = userList});
        }
        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {

            var objFromDb = unitOfWork.ApplicationUsers.Get(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //The user's lock period has ended.
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddMonths(24);
            }
            unitOfWork.ApplicationUsers.Update(objFromDb);
            unitOfWork.Save();
            return Json(new { success = true, message = "Operation Successful" });
        }

        #endregion
    }
}
