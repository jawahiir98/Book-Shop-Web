using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Models.ViewModels;
using BookShop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BookShopWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        [BindProperty]
        public OrderVM orderVM { get; set; }
        public OrderController(IUnitOfWork _unitOfWork)
        {
            unitOfWork = _unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int orderId)
        {
            orderVM = new() {
                OrderHeader = unitOfWork.OrderHeaders.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = unitOfWork.OrderDetails.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(orderVM);   
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = unitOfWork.OrderHeaders.Get(u => u.Id == orderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
            orderHeaderFromDb.City = orderVM.OrderHeader.City;
            orderHeaderFromDb.State = orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;
            orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
            if (!String.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }
            if (!String.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            }
            unitOfWork.OrderHeaders.Update(orderHeaderFromDb);
            unitOfWork.Save();
            TempData["success"] = "Order updated successfully.";
            return RedirectToAction(nameof(Index), new { orderID = orderVM.OrderHeader.Id });
        }
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaderList;
            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaderList = unitOfWork.OrderHeaders.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                orderHeaderList = unitOfWork.OrderHeaders.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "inprocess":
                    orderHeaderList = orderHeaderList.Where(u => u.PaymentStatus == SD.StatusInProcess);
                    break;
                case "pending":
                    orderHeaderList = orderHeaderList.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "approved":
                    orderHeaderList = orderHeaderList.Where(u => u.PaymentStatus == SD.StatusApproved);
                    break;
                case "completed":
                    orderHeaderList = orderHeaderList.Where(u => u.PaymentStatus == SD.StatusShipped);
                    break;
                default:
                    break;
            }

            return Json(new { data = orderHeaderList });
        }
        #endregion

    }
}
