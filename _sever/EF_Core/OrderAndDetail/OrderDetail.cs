namespace _sever.EF_Core.OrderAndDetail
{
    public class OrderDetail
    {
        public Guid Id { get; set; }
        public string CuisineName { get; set; }
        public decimal CuisinePrice { get; set; }
        public int Number { get; set; }
        public decimal Amout { get; set; }
        public Guid OrderId { get; set; }
        public Order Order { get; set; }
    }
}
