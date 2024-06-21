using _sever.EF_Core.CuisineMenu;

namespace _sever.entity
{
    public class WXOrderDetailDto
    {
        public int Id { get; set; }
        public int? T_CuisineType_Id { get; set; }
        public string OrderDetailName { get; set; }
        public string OrderDetailPictureUrl { get; set; }
        public decimal OrderDetailPrice { get; set; }
        public int Number { get; set; }
        public decimal Amount { get; set; }
        public List<CustomerInfo> CustomerInfos { get; set; }
    }
}
