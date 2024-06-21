using _sever.EF_Core.OrderAndDetail;
using Microsoft.AspNetCore.Mvc;


namespace _sever.Controllers
{
    [Controller]
    [Route("[controller]/[action]")]
    public class OrderController:ControllerBase
    {
        private readonly OrderDbContext orderDbContext;
        
        public OrderController(OrderDbContext _orderDbContext) { 
            this.orderDbContext = _orderDbContext;
        }

        [HttpGet]
        public IActionResult GetOrders(int PayState,int CurrentPage, int PageSize) {
            Console.WriteLine(PayState);
            Console.WriteLine(CurrentPage);
            Console.WriteLine(PageSize);
            /*查询未支付的订单*/
            if (PayState == 0)  
            {
                IQueryable<Order> orders = orderDbContext.Orders.Where(order => order.PayState == 0);
                Order[] _orders = orders.Skip((CurrentPage - 1)*PageSize).Take(PageSize).ToArray();

                foreach (var order in _orders)
                {
                    order.OpenId = "";
                }

                var res = new { dataNumber = orders.ToArray().Length,orders=_orders };
                return Ok(res);
            }if (PayState == 1)
            {
                Order[] orders = orderDbContext.Orders.Where(order => order.PayState == 1).ToArray();
                Order[] _orders = orders.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToArray();
                foreach (var order in _orders)
                {
                    order.OpenId = "";
                }
                var res = new { dataNumber = orders.ToArray().Length, orders = _orders };
                return Ok(res);
            }
            return BadRequest();
        }
        [HttpGet]
        public IActionResult GetOrderDetailById(string Id)
        {
            Guid id = new Guid(Id);
            OrderDetail[] orderDetails = orderDbContext.OrderDetails.Where(orderDetail => orderDetail.OrderId== id).ToArray();
            return Ok(orderDetails);

        }
        [HttpGet]
        public async Task<IActionResult> FinishOrderById(string Id)
        {
            Guid id = new Guid(Id);
            Order order = orderDbContext.Orders.Single(order => order.Id == id);
            order.PayState = 1;
            orderDbContext.SaveChanges();
            return Ok("订单已完成");
        }
    }
}
