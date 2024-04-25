using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BookShopWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        public OrderController(IUnitOfWork _unitOfWork)
        {
            unitOfWork = _unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaderList = unitOfWork.OrderHeaders.GetAll(includeProperties: "ApplicationUser").ToList();

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
