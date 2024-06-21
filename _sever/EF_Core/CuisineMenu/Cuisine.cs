namespace _sever.EF_Core.CuisineMenu
{
    public class Cuisine
    {
        public int Id { get; set; }
        public string CuisineName { get; set; }
        public string CuisinePictureUrl { get; set; }
        public decimal CuisinePrice { get; set; }
        public string? CuisineDescription { get; set; }

        /*外键及关联对象*/
        public int? T_CuisineType_Id { get; set; }
        public CuisineType? Cuisine_Type { get; set; }

        public int Number { get; set; }
        public object[]? CustomerInfos { get; set; }=new object[0];

    }
}
