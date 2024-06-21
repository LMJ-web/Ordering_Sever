using _sever.EF_Core.CuisineMenu;

namespace _sever.EF_Core.OrderAndDetail
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTime DateTime { get; set; }
        public string TableNo { get; set; }
        public string NickName { get; set; }
        public string OpenId { get; set; }
        public decimal Total { get; set; }
        public int? PayState { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
    }
}
