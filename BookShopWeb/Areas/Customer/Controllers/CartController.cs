using BookShop.DataAccess.Repository;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using BookShop.Models.ViewModels;
using BookShop.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Security.Claims;

namespace BookShopWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        [BindProperty]
        public ShoppingCartVM CartVM { get; set; }
        public CartController(IUnitOfWork _unitOfWork)
        {
            unitOfWork = _unitOfWork;
        }
        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50) return shoppingCart.Product.Price;
            else
            {
                if (shoppingCart.Count < 100) return shoppingCart.Product.Price50;
                else return shoppingCart.Product.Price100;
            }
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            CartVM = new()
            {
                ShoppingCartList = unitOfWork.ShoppingCarts.GetAll(
                    u => u.ApplicationUserId == userId,
                    includeProperties: "Product"
                ),
                OrderHeader = new()
            };
            foreach (var cart in CartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                CartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }
            return View(CartVM);
        }
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            CartVM = new()
            {
                ShoppingCartList = unitOfWork.ShoppingCarts.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            CartVM.OrderHeader.ApplicationUser = unitOfWork.ApplicationUsers.Get(u => u.Id == userId);

            CartVM.OrderHeader.Name = CartVM.OrderHeader.ApplicationUser.Name;
            CartVM.OrderHeader.StreetAddress = CartVM.OrderHeader.ApplicationUser.StreetAddress;
            CartVM.OrderHeader.State = CartVM.OrderHeader.ApplicationUser.State;
            CartVM.OrderHeader.City = CartVM.OrderHeader.ApplicationUser.City;
            CartVM.OrderHeader.PostalCode = CartVM.OrderHeader.ApplicationUser.PostalCode;
            CartVM.OrderHeader.PhoneNumber = CartVM.OrderHeader.ApplicationUser.PhoneNumber;

            foreach (var cart in CartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                CartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(CartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            CartVM.ShoppingCartList = unitOfWork.ShoppingCarts.GetAll(
                u => u.ApplicationUserId == userId,
                includeProperties: "Product"
             );

            CartVM.OrderHeader.OrderDate = System.DateTime.Now;
            CartVM.OrderHeader.ApplicationUserId = userId;

            ApplicationUser appUser = unitOfWork.ApplicationUsers.Get(u => u.Id == userId);


            foreach (var cart in CartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                CartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            if (appUser.CompanyId.GetValueOrDefault() == 0)
            {
                // Customer order management process 
                CartVM.OrderHeader.OrderStatus = SD.StatusPending;
                CartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            }
            else
            {
                // Company order management process
                CartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                CartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            }

            unitOfWork.OrderHeaders.Add(CartVM.OrderHeader);
            unitOfWork.Save();

            foreach (var cart in CartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = CartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                unitOfWork.OrderDetails.Add(orderDetail);
                unitOfWork.Save();
            }

            if (appUser.CompanyId.GetValueOrDefault() == 0)
            {
                // Strie logic, customer user
                var domain = "https://localhost:7058/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={CartVM.OrderHeader.Id}",
                    CancelUrl = domain + "customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };
                foreach (var item in CartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100), // $ 20.50 = 2050
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
                unitOfWork.OrderHeaders.UpdateStripePaymentId(CartVM.OrderHeader.Id,session.Id, session.PaymentIntentId);
                unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            
            return RedirectToAction(nameof(OrderConfirmation), new { id = CartVM.OrderHeader.Id });
        }
        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = unitOfWork.OrderHeaders.Get(u => u.Id == id, includeProperties:"ApplicationUser");
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // The order belongs to a customer user.
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if(session.PaymentStatus.ToLower() == "paid")
                {
                    unitOfWork.OrderHeaders.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    unitOfWork.OrderHeaders.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    unitOfWork.Save();
                }
            }
            List<ShoppingCart> shoppingCarts = unitOfWork.ShoppingCarts.GetAll(
                    u => u.ApplicationUserId == orderHeader.ApplicationUserId
                ).ToList();
            unitOfWork.ShoppingCarts.RemoveRange(shoppingCarts);
            unitOfWork.Save();

            return View(id);
        }
        public IActionResult Plus(int CartId)
        {
            var cartFromDb = unitOfWork.ShoppingCarts.Get(u => u.Id == CartId);
            cartFromDb.Count += 1;
            unitOfWork.ShoppingCarts.Update(cartFromDb);
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int CartId)
        {
            var cartFromDb = unitOfWork.ShoppingCarts.Get(u => u.Id == CartId);
            if (cartFromDb.Count <= 1)
            {
                unitOfWork.ShoppingCarts.Remove(cartFromDb);

                HttpContext.Session.SetInt32(
                    SD.SessionCart,
                    unitOfWork.ShoppingCarts.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() -1
                );
            }
            else
            {
                cartFromDb.Count -= 1;
            }
            unitOfWork.ShoppingCarts.Update(cartFromDb);
            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int CartId)
        {
            var cartFromDb = unitOfWork.ShoppingCarts.Get(u => u.Id == CartId);

            unitOfWork.ShoppingCarts.Remove(cartFromDb);

            HttpContext.Session.SetInt32(
                SD.SessionCart,
                unitOfWork.ShoppingCarts.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1
            );

            unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
    }
}
