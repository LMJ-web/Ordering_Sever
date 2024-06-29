using _sever.Dto;
using _sever.EF_Core.CuisineMenu;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace _sever.Controllers.WXMiniProgram
{
    [ApiController]
    [Route("[controller]/[action]")]
    [ResponseCache(Duration = 30)] //建议客户端开启客户端缓存
    public class WX_CuisineController:ControllerBase
    {
        private readonly CuisineDbContext cuisineDbContext;
        private readonly IDistributedCache redisCache;
        private readonly IMemoryCache memoryCache;
        

        public WX_CuisineController(CuisineDbContext cuisineDbContext, IDistributedCache redisCache,IMemoryCache memoryCache)
        {
            this.cuisineDbContext = cuisineDbContext;
            this.redisCache = redisCache;
            this.memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<IActionResult> GetCuisineTypes()
        {
            //redis缓存
            string cuisineTypesJson = await redisCache.GetStringAsync("WX_CuisineType");
            CuisineType[] cuisineTypes = null;
            if (cuisineTypesJson != null)
            {
                //反序列化
                cuisineTypes = Newtonsoft.Json.JsonConvert.DeserializeObject<CuisineType[]>(cuisineTypesJson);
                return Ok(cuisineTypes);
            }
            cuisineTypes = await cuisineDbContext.CuisineTypes.OrderBy(cuisineType=>cuisineType.PriorityLevel).ToArrayAsync();
            //序列化
            cuisineTypesJson = Newtonsoft.Json.JsonConvert.SerializeObject(cuisineTypes);
            var options = new DistributedCacheEntryOptions();
            options.AbsoluteExpiration = DateTime.Now.AddHours(8);
            options.SlidingExpiration = TimeSpan.FromSeconds(30);
            //设置redis缓存
            await redisCache.SetStringAsync("WX_CuisineType", cuisineTypesJson, options);
            return Ok(cuisineTypes);

        }
        [HttpGet]
        public async Task<IActionResult> GetCuisines()
        {
            //redis缓存
            string cuisinesJson = await redisCache.GetStringAsync("WX_Cuisines");

            if (cuisinesJson != null)
            {
                return Ok(Newtonsoft.Json.JsonConvert.DeserializeObject<List<WXCuisineDto>>(cuisinesJson));
            }
            Cuisine[] cuisines = cuisineDbContext.Cuisines.Include(cuisine => cuisine.Cuisine_Type).OrderBy(cuisine => cuisine.Cuisine_Type.PriorityLevel).ToArray();
            IQueryable cuisineTypes = cuisineDbContext.CuisineTypes.OrderBy(cuisineType => cuisineType.PriorityLevel);
            IEnumerator<CuisineType> enumerator = (IEnumerator<CuisineType>)cuisineTypes.GetEnumerator();
            var WXCuisineDtoList = new List<WXCuisineDto>();
            while (enumerator.MoveNext())
            {
                WXCuisineDto wXCuisineDto = new WXCuisineDto();
                wXCuisineDto.CuisineType = enumerator.Current;
                Cuisine[] sameTypeCuisins = cuisines.Where(cuisine => cuisine.T_CuisineType_Id == enumerator.Current.Id).ToArray();
                wXCuisineDto.SameTypeCuisines = sameTypeCuisins;
                WXCuisineDtoList.Add(wXCuisineDto);
            }
            cuisinesJson = Newtonsoft.Json.JsonConvert.SerializeObject(WXCuisineDtoList);
            var options = new DistributedCacheEntryOptions();
            options.AbsoluteExpiration = DateTime.Now.AddHours(8);
            options.SlidingExpiration = TimeSpan.FromSeconds(30);
            await redisCache.SetStringAsync("WX_Cuisines", cuisinesJson, options);
            return Ok(WXCuisineDtoList);
        }
    }
}
