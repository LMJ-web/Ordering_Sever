using _sever.EF_Core.OrderAndDetail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace _sever.Controllers.WXMiniProgram
{
    [Controller]
    [Route("[controller]/[action]")]
    public class WX_OrderController : ControllerBase
    {
        private readonly OrderDbContext orderDbContext;
        private readonly IDistributedCache redis;
        public WX_OrderController(OrderDbContext orderDbContext, IDistributedCache redis)
        {
            this.orderDbContext = orderDbContext;
            this.redis = redis;
        }
        [HttpGet]
        public IActionResult GetOrderHistory(string session_key)
        {
            Console.WriteLine(session_key);
            string openid = redis.GetString(session_key);
            if (openid == null || session_key == null) { return BadRequest("登录过期，请先登录"); }
            Order[] orders = orderDbContext.Orders.Where(order => order.OpenId == openid).OrderByDescending(order=>order.DateTime).ToArray();
            if (orders.Length == 0) { return BadRequest("无数据"); }
            return Ok(orders);
        }
        [HttpGet]
        public IActionResult GetOrderHistoryDetailById(string orderId) {
            if (orderId == null) { return BadRequest("无效订单Id"); }
            Guid _orderId = new Guid(orderId);
            OrderDetail[] orderDetails = orderDbContext.OrderDetails.Where(orderDetail=>orderDetail.OrderId == _orderId).ToArray();
            return Ok(orderDetails);
        }
    }
}
