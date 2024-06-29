using _sever.EF_Core.CuisineMenu;

namespace _sever.Dto
{
    public class WXCuisineDto
    {
        public CuisineType CuisineType { get; set; }
        public Cuisine[] SameTypeCuisines { get; set; }
    }
}
