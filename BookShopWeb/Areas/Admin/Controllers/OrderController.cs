using BookShop.DataAccess.Repository;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Models.ViewModels;
using BookShop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Diagnostics;
using System.Security.Claims;

namespace BookShopWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
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
        [ActionName("Details")]
        public IActionResult Details_PAY_NOW()
        {
            orderVM.OrderHeader = unitOfWork.OrderHeaders
                .Get(u => u.Id == orderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            orderVM.OrderDetail = unitOfWork.OrderDetails
                .GetAll(u => u.OrderHeaderId == orderVM.OrderHeader.Id, includeProperties: "Product");

            //stripe logic
            var domain = "https://localhost:7169/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={orderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={orderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in orderVM.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }


            var service = new SessionService();
            Session session = service.Create(options);
            unitOfWork.OrderHeaders.UpdateStripePaymentId(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {

            OrderHeader orderHeader = unitOfWork.OrderHeaders.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //this is an order by company

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    unitOfWork.OrderHeaders.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    unitOfWork.OrderHeaders.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    unitOfWork.Save();
                }

            }

            return View(orderHeaderId);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = unitOfWork.OrderHeaders.Get(u => u.Id == orderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = orderVM.OrderHeader.City;
            orderHeaderFromDb.State = orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.TrackingNumber;
            }
            unitOfWork.OrderHeaders.Update(orderHeaderFromDb);
            unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";


            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            unitOfWork.OrderHeaders.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {

            var orderHeader = unitOfWork.OrderHeaders.Get(u => u.Id == orderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = orderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            unitOfWork.OrderHeaders.Update(orderHeader);
            unitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {

            var orderHeader = unitOfWork.OrderHeaders.Get(u => u.Id == orderVM.OrderHeader.Id);
            
            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(options);

                unitOfWork.OrderHeaders.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
               
                TempData["Success"] = "Your order has been cancelled successfully!";
            }
            else
            {
                unitOfWork.OrderHeaders.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            unitOfWork.Save();
            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
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
