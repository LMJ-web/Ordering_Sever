using Microsoft.AspNetCore.Mvc;
using _sever.entity;
using _sever.EF_Core.CuisineMenu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;

namespace _sever.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CuisineController: ControllerBase
    {
        public CuisineDbContext cuisineDbContext { get; set; }
        public IDistributedCache redisCache { get; set; }
        public readonly FileExtensionContentTypeProvider provider;

        public CuisineController(IDistributedCache redisCache, CuisineDbContext cuisineDbContext, FileExtensionContentTypeProvider provider)
        {
            this.redisCache = redisCache;
            this.cuisineDbContext = cuisineDbContext;
            this.provider = provider;
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddCuisine(CuisineVo cuisineVo) {
            //将图片存储到本地
            byte[] pictureBytes = await redisCache.GetAsync(cuisineVo.CuisinePictureKey);
            FileStream fileStream = new FileStream("D:\\VisualStudioProject\\_sever\\_sever\\StaticDataSource\\" + cuisineVo.CuisinePictureKey, FileMode.Create);
            await fileStream.WriteAsync(pictureBytes);
            fileStream.Close();
            //将url存储到数据库中
            string cuisinePictureUrl = "https://localhost:7106/StaticFiles/" + cuisineVo.CuisinePictureKey;
            Cuisine cuisine = new Cuisine
            {
                CuisineName = cuisineVo.CuisineName,
                CuisinePictureUrl = cuisinePictureUrl,
                CuisinePrice = cuisineVo.CuisinePrice,
                T_CuisineType_Id = cuisineVo.CuisineType_Id,
                CuisineDescription = cuisineVo.CuisineDescription,
            };
            EntityEntry<Cuisine> entityEntry = await cuisineDbContext.AddAsync(cuisine);
            if (entityEntry.State != EntityState.Added)
            {
                return BadRequest("添加失败");
            }
            _= await cuisineDbContext.SaveChangesAsync();
            return Ok("添加成功！");
        }
        [HttpPost]
        [Authorize(Roles = "admin")]
        public IActionResult UploadPicture(IFormFile file)
        {
            if(!provider.TryGetContentType(file.FileName, out string contentType))
            {
                return BadRequest("上传的文件类型错误！");
            }
            //获取图片的字节数组
            var fileBytes = new byte[file.Length];
            Stream stream = file.OpenReadStream();
            stream.Read(fileBytes);

            //将字节数组存放进redis中
            string cuisinePictureKey = file.Length.ToString()+"-"+file.FileName;
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            redisCache.SetAsync(cuisinePictureKey, fileBytes, options);
            return Ok("图片已缓存");
        }
        [HttpGet]
        [Authorize(Roles = "admin,common")]
        public IActionResult GetCuisineTypes() {
            CuisineType[] cuisineTypes = cuisineDbContext.CuisineTypes.ToArray();
            return Ok(cuisineTypes);
        }

        [HttpGet]
        [Authorize(Roles = "admin,common")]
        public IActionResult GetCuisines() {
            /*return Ok(cuisineDbContext.Cuisines.ToArray());*/
            Cuisine[] cuisines = cuisineDbContext.Cuisines.Include(cuisine => cuisine.Cuisine_Type).OrderBy(cuisine => cuisine.Cuisine_Type.PriorityLevel).ToArray();
            return Ok(cuisines);
        }

        [HttpGet]
        [Authorize(Roles = "admin,common")]
        public IActionResult SearchCuisineByName(string? cuisineName)
        {
            if (string.IsNullOrEmpty(cuisineName)) {
                return Ok(cuisineDbContext.Cuisines.ToArray());
            }
            Cuisine[] cuisines = cuisineDbContext.Cuisines.Where(x => x.CuisineName.Contains(cuisineName)).ToArray();
            return Ok(cuisines);
        }

        [HttpPatch]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateCuisine(CuisineVo cuisineVo)
        {
            Cuisine cuisine = cuisineDbContext.Cuisines.SingleOrDefault(x => x.Id == cuisineVo.Id);
            if (cuisine == null) return BadRequest("菜品不存在");
            cuisine.CuisineName = cuisineVo.CuisineName;
            cuisine.CuisinePrice = cuisineVo.CuisinePrice;
            cuisine.T_CuisineType_Id = cuisineVo.CuisineType_Id;
            cuisine.CuisineDescription = cuisineVo.CuisineDescription;
            if (!string.IsNullOrEmpty(cuisineVo.CuisinePictureKey))
            {
                //将图片存储到本地
                byte[] pictureBytes = await redisCache.GetAsync(cuisineVo.CuisinePictureKey);
                FileStream fileStream = new FileStream("D:\\VisualStudioProject\\_sever\\_sever\\StaticDataSource\\" + cuisineVo.CuisinePictureKey, FileMode.Create);
                await fileStream.WriteAsync(pictureBytes);
                fileStream.Close();

                //将url存储到数据库中
                string cuisinePictureUrl = "https://localhost:7106/StaticFiles/" + cuisineVo.CuisinePictureKey;
                cuisine.CuisinePictureUrl = cuisinePictureUrl;
            }
            cuisineDbContext.SaveChanges();
            return Ok("修改成功");
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public IActionResult DeleteCuisineById(int id)
        {
            Cuisine cuisine = cuisineDbContext.Cuisines.SingleOrDefault(c => c.Id == id);
            if (cuisine != null) {
                EntityEntry<Cuisine> entityEntry = cuisineDbContext.Remove(cuisine);
                if (entityEntry.State == EntityState.Deleted) {
                    cuisineDbContext.SaveChanges();
                    return Ok("删除成功");
                }
                else { return BadRequest("删除失败"); }
            }
            return BadRequest("菜品不存在");
            
        }
    }
}
