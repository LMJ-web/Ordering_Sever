using _sever.Dto;
using _sever.EF_Core.CuisineMenu;
using _sever.EF_Core.OrderAndDetail;
using _sever.entity;
using _sever.Vo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Transactions;

namespace _sever.Controllers.WXMiniProgram
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WX_ShoppingCarController : ControllerBase
    {
        private readonly IDistributedCache redisCache;
        private readonly IMemoryCache memoryCache;
        private readonly CuisineDbContext cuisineDbContext;
        private Dictionary<string, AutoResetEvent> autoResetEventDic;
        private Dictionary<string, List<WXShoppingCarDto>> sameTableShoppingCar;
        private readonly OrderDbContext orderDbContext;
        public WX_ShoppingCarController(IDistributedCache redisCache, IMemoryCache memoryCache, CuisineDbContext cuisineDbContext, Dictionary<string, AutoResetEvent> autoResetEventDic, Dictionary<string, List<WXShoppingCarDto>> sameTableShoppingCar, OrderDbContext orderDbContext)
        {
            this.redisCache = redisCache;
            this.memoryCache = memoryCache;
            this.cuisineDbContext = cuisineDbContext;
            this.autoResetEventDic = autoResetEventDic;
            this.sameTableShoppingCar = sameTableShoppingCar;
            this.orderDbContext = orderDbContext;
        }
        [HttpPost]
        public IActionResult AddCuisine(WXShoppingCarVo wXShoppingCarVo) {
            if (string.IsNullOrEmpty(wXShoppingCarVo.Session_key)) { return BadRequest("请先登录"); }
            string openId = redisCache.GetString(wXShoppingCarVo.Session_key);
            if (string.IsNullOrEmpty(openId)) { return BadRequest("登录过期，请重新登录"); }
            //对openid加密，用于唯一表示点餐的顾客
            byte[] md5_bytes = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(openId));
            string md5_openId = System.Text.Encoding.UTF8.GetString(md5_bytes);
            //使用内存缓存，减少查询数据库的频率
            List<Cuisine> cuisinesForCreateShoppingCar = memoryCache.GetOrCreate("cuisinesForCreateShoppingCar", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                return cuisineDbContext.Cuisines.ToList();
            });
            Cuisine? cuisine = cuisinesForCreateShoppingCar.SingleOrDefault(cuisine => cuisine.Id == wXShoppingCarVo.Id);
            if (cuisine == null) return BadRequest("cuisine的Id不存在");
            string tableNo = wXShoppingCarVo.TableNo;
            //点餐过程加锁，保持数据一致
            AutoResetEvent? autoResetEvent;
            if (!autoResetEventDic.TryGetValue(tableNo, out autoResetEvent))
            {   //首次点餐，初始化锁资源
                autoResetEventDic.Add(wXShoppingCarVo.TableNo, new AutoResetEvent(true));
            }
            bool isHaveLock = autoResetEventDic[tableNo].WaitOne(TimeSpan.FromSeconds(1));
            if (!isHaveLock)
            {

                return BadRequest("订单提交中,请勿点餐");
            }
            else {
                List<WXShoppingCarDto>? wXShoppingCarDtos;
                if (!sameTableShoppingCar.TryGetValue(tableNo,out wXShoppingCarDtos))
                {
                    //购物车不存在，初始化购物车
                    WXShoppingCarDto wXShoppingCarDto = new WXShoppingCarDto
                    {
                        Id = cuisine.Id,
                        T_CuisineType_Id = cuisine.T_CuisineType_Id,
                        CuisineName = cuisine.CuisineName,
                        CuisinePictureUrl = cuisine.CuisinePictureUrl,
                        CuisinePrice = cuisine.CuisinePrice,
                        Number = 1,
                        Amount = cuisine.CuisinePrice,
                        CustomerInfos = new List<CustomerInfo>()
                    };
                    wXShoppingCarDto.CustomerInfos.Add(new CustomerInfo
                    {
                        Nickname = wXShoppingCarVo.Nickname,
                        AvatarUrl = wXShoppingCarVo.AvatarUrl,
                        Number = 1,
                        MD5_OpenId = md5_openId
                    });
                    wXShoppingCarDtos = new List<WXShoppingCarDto>
                    {
                        wXShoppingCarDto
                    };
                    sameTableShoppingCar.Add(tableNo, wXShoppingCarDtos);
                }
                else {
                    //购物车已存在
                    int cuisineIndex = sameTableShoppingCar[tableNo].FindIndex(wXShoppingCarDto => wXShoppingCarDto.Id == wXShoppingCarVo.Id);
                    if(cuisineIndex == -1)
                    {   //往购物车内新添一条菜品信息
                        WXShoppingCarDto wXShoppingCarDto =new WXShoppingCarDto { Id = wXShoppingCarVo.Id,CuisineName = cuisine.CuisineName, CuisinePictureUrl = cuisine.CuisinePictureUrl, CuisinePrice = cuisine.CuisinePrice,Number = 1, Amount = cuisine.CuisinePrice,T_CuisineType_Id = cuisine.T_CuisineType_Id, CustomerInfos = new List<CustomerInfo>() };
                        wXShoppingCarDto.CustomerInfos.Add(new CustomerInfo { Nickname = wXShoppingCarVo.Nickname, AvatarUrl = wXShoppingCarVo.AvatarUrl,Number = 1,MD5_OpenId = md5_openId });
                        sameTableShoppingCar[tableNo].Add(wXShoppingCarDto);
                    }
                    else
                    {
                        //改变菜品的数量
                        sameTableShoppingCar[tableNo][cuisineIndex].Number += 1;
                        sameTableShoppingCar[tableNo][cuisineIndex].Amount += cuisine.CuisinePrice;
                        int customerIndex = sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos.FindIndex(customerInfo => customerInfo.MD5_OpenId == md5_openId);
                        if (customerIndex == -1) {
                            sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos.Add(new CustomerInfo { Nickname = wXShoppingCarVo.Nickname, AvatarUrl = wXShoppingCarVo.AvatarUrl, Number = 1 });
                        }
                        else
                        {
                            sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos[customerIndex].Number += 1;
                            if (sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos[customerIndex].Nickname != wXShoppingCarVo.Nickname || sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos[customerIndex].AvatarUrl != wXShoppingCarVo.AvatarUrl)
                            {//顾客点餐后，修改了自己的昵称或头像
                                sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos[customerIndex].Nickname = wXShoppingCarVo.Nickname;
                                sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos[customerIndex].AvatarUrl = wXShoppingCarVo.AvatarUrl;
                            }
                        }
                    }   
                }
                autoResetEventDic[tableNo].Set();
            }
            return Ok(sameTableShoppingCar);
        }
        [HttpPost]
        public IActionResult SubCuisine(WXShoppingCarVo wXShoppingCarVo)
        {
            if (string.IsNullOrEmpty(wXShoppingCarVo.Session_key)) { return BadRequest("请先登录"); }
            string openId = redisCache.GetString(wXShoppingCarVo.Session_key);
            if (string.IsNullOrEmpty(openId)) { return BadRequest("登录过期，请重新登录"); }
            //对openid加密，用于唯一表示点餐的顾客
            byte[] md5_bytes = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(openId));
            string md5_openId = System.Text.Encoding.UTF8.GetString(md5_bytes);
            //使用内存缓存，减少查询数据库的频率
            List<Cuisine> cuisinesForCreateOrderDetail = memoryCache.GetOrCreate("cuisinesForCreateShoppingCar", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                return cuisineDbContext.Cuisines.ToList();
            });
            Cuisine? cuisine = cuisinesForCreateOrderDetail.SingleOrDefault(cuisine => cuisine.Id == wXShoppingCarVo.Id);
            if (cuisine == null) return BadRequest("cuisine的Id不存在");
            string tableNo = wXShoppingCarVo.TableNo;
            List<WXShoppingCarDto>? wXShoppingCarDtos;
            if(!sameTableShoppingCar.TryGetValue(tableNo, out wXShoppingCarDtos)){ return BadRequest("购物车不存在,请先扫码");}
            AutoResetEvent? autoResetEventOfTablle;
            if (!autoResetEventDic.TryGetValue(tableNo, out autoResetEventOfTablle)) { return BadRequest("请先点餐"); }

            bool isHaveLock = autoResetEventDic[tableNo].WaitOne(TimeSpan.FromSeconds(1));
            if (!isHaveLock) { return BadRequest("订单提交中，请勿点餐"); }

            int cuisineIndex = sameTableShoppingCar[tableNo].FindIndex(wXShoppingCarDto => wXShoppingCarDto.Id == cuisine.Id);
            if(cuisineIndex == -1) { return BadRequest("尚未选购该菜品，请先点餐"); }
            if (sameTableShoppingCar[tableNo][cuisineIndex].Number <= 0) { autoResetEventDic[tableNo].Set(); return BadRequest("菜品数量不可小于0"); }
            sameTableShoppingCar[tableNo][cuisineIndex].Number -= 1;

            if(sameTableShoppingCar[tableNo][cuisineIndex].Number >= 0) {
                sameTableShoppingCar[tableNo][cuisineIndex].Amount -= cuisine.CuisinePrice;
                int customerIndex = sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos.FindIndex(customerInfo => customerInfo.MD5_OpenId == md5_openId);
                if (customerIndex <= -1) { autoResetEventDic[tableNo].Set(); return BadRequest("账号信息错误，请先登录"); }
                sameTableShoppingCar[tableNo][cuisineIndex].CustomerInfos[customerIndex].Number -= 1;
            }
            else
            {
                sameTableShoppingCar[tableNo].RemoveAt(cuisineIndex);
            }
            autoResetEventDic[tableNo].Set();
            return Ok(sameTableShoppingCar);
        }
        [HttpGet]
        public IActionResult GetSameTableShoppingCar(string tableNo)
        {
            List<WXShoppingCarDto>? wXShoppingCarDtos;
            sameTableShoppingCar.TryGetValue(tableNo, out wXShoppingCarDtos);
            return Ok(wXShoppingCarDtos);
        }
        [HttpGet]
        public IActionResult LockOrder(string tableNo)
        {
            if (string.IsNullOrEmpty(tableNo)) return BadRequest("tableNo不能为空");
            AutoResetEvent? autoResetEvent;
            if (!autoResetEventDic.TryGetValue(tableNo, out autoResetEvent))
            {
                autoResetEventDic.Add(tableNo, new AutoResetEvent(true));
            }
            autoResetEventDic[tableNo].WaitOne();
            return Ok("订单锁定中");
        }

        [HttpGet]
        public IActionResult UnLockOrder(string tableNo)
        {
            AutoResetEvent? autoResetEvent;
            if (!autoResetEventDic.TryGetValue(tableNo, out autoResetEvent)) return Ok($"{tableNo}订单未锁定，不需要解锁");
            bool UnLockResult = autoResetEvent.Set();
            if (!UnLockResult) return BadRequest($"解除锁定{tableNo}的订单失败");
            return Ok($"Unlock {tableNo}订单成功");
        }
        [HttpPost]
        public IActionResult SubmitOrder(WxOrderVo wxOrderVo)
        {
            if (string.IsNullOrEmpty(wxOrderVo.Session_key)) { return BadRequest("请先登录"); }
            string openId = redisCache.GetString(wxOrderVo.Session_key);
            if (string.IsNullOrEmpty(openId)) { return BadRequest("登录过期，请重新登录"); }
            if (string.IsNullOrEmpty(wxOrderVo.TableNo)) { return BadRequest("请先扫码"); }

            List<WXShoppingCarDto>? wXShoppingCarDtos;
            _ = sameTableShoppingCar.TryGetValue(wxOrderVo.TableNo, out wXShoppingCarDtos);
            if (wXShoppingCarDtos == null) { return BadRequest("请先点餐"); }
            Order order = new Order { DateTime = DateTime.Now, TableNo = wxOrderVo.TableNo, OpenId = openId, NickName = wxOrderVo.Nickname };
            order.Total = 0;
            order.PayState = 0;
            List<OrderDetail> orderDetailList = new List<OrderDetail>();
            using (TransactionScope tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    wXShoppingCarDtos.ForEach(wXShoppingCarDto =>
                    {
                        if (wXShoppingCarDto.Number > 0) {
                            OrderDetail orderDetail = new OrderDetail();
                            orderDetail.CuisineName = wXShoppingCarDto.CuisineName;
                            orderDetail.CuisinePrice = wXShoppingCarDto.CuisinePrice;
                            orderDetail.Number = wXShoppingCarDto.Number;
                            orderDetail.Amout = wXShoppingCarDto.Amount;
                            order.Total += wXShoppingCarDto.Amount;
                            orderDbContext.OrderDetails.Add(orderDetail);
                            orderDetailList.Add(orderDetail);
                        }
                    });
                    order.OrderDetails = orderDetailList;
                    orderDbContext.Orders.Add(order);
                    orderDbContext.SaveChanges();
                    tx.Complete();
                    return Ok(wXShoppingCarDtos);
                }
                catch (Exception ex)
                {
                    return BadRequest("网络不佳，请重尝试");
                }
                finally { tx.Dispose(); }
            }
        }
    }
}
