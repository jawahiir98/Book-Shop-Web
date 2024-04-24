using BookShop.DataAccess.Data;
using BookShop.DataAccess.Repository.IRepository;
using BookShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookShop.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderHeader orderHeader)
        {
            _db.OrderHeaders.Update(orderHeader);
        }
        public void UpdateStatus(int id, string orderStatus, string? paymentStatus)
        {
            var orderObj = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
            if (orderObj != null)
            {
                orderObj.OrderStatus = orderStatus;
                if (!string.IsNullOrEmpty(paymentStatus)) orderObj.PaymentStatus = paymentStatus;
            }
        }
        public void UpdateStripePaymentId(int id, string sessionId, string PaymentIntentId)
        {
            var orderObj = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
            if (!string.IsNullOrEmpty(sessionId))
            {
                orderObj.SessionId = sessionId;
            }
            if (!string.IsNullOrEmpty(PaymentIntentId))
            {
                orderObj.PaymentIntentId = PaymentIntentId;
                orderObj.PaymentDate = DateTime.Now;
            }
        }
    }
}
