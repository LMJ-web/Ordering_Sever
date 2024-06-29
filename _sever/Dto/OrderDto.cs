namespace _sever.Dto
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public DateTime DateTime { get; set; }
        public string TableNo { get; set; }
        public string NickName { get; set; }
        public decimal Total { get; set; }
    }
}
