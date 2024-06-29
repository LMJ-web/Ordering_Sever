using _sever.entity;

namespace _sever.Dto
{
    public class WXShoppingCarDto
    {
        public int Id { get; set; }
        public int? T_CuisineType_Id { get; set; }
        public string CuisineName { get; set; }
        public string CuisinePictureUrl { get; set; }
        public decimal CuisinePrice { get; set; }
        public int Number { get; set; }
        public decimal Amount { get; set; }
        public List<CustomerInfo> CustomerInfos { get; set; }
    }
}
