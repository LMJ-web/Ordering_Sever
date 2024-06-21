namespace _sever.entity
{
    public class CuisineVo
    {
        public int? Id { get; set; }
        public string CuisineName { get; set; }
        public string? CuisinePictureKey { get; set; }
        public decimal CuisinePrice { get; set; }
        public string? CuisineDescription { get; set; }
        public int CuisineType_Id { get; set; }
    }
}
