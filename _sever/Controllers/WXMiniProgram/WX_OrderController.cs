using _sever.EF_Core.CuisineMenu;
using _sever.EF_Core.OrderAndDetail;
using _sever.entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Transactions;
using System.Security.Cryptography;

namespace _sever.Controllers.WXMiniProgram
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WX_OrderController : ControllerBase
    {
        private readonly OrderDbContext orderDbContext;
        private readonly CuisineDbContext cuisineDbContext;
        private readonly IDistributedCache redis_cache;
        private readonly IMemoryCache memoryCache;
        private Dictionary<string, List<WXOrderDetailDto>> sameTableOrderDetailsDic;
        private Dictionary<string, AutoResetEvent> autoResetEventDic;

        public WX_OrderController(OrderDbContext orderDbContext, IDistributedCache redis_cache,Dictionary<string, List<WXOrderDetailDto>> sameTableOrderDetailsDic,IMemoryCache memoryCache, CuisineDbContext cuisineDbContext, Dictionary<string, AutoResetEvent> autoResetEventDic)
        {
            this.orderDbContext = orderDbContext;
            this.redis_cache = redis_cache;
            this.sameTableOrderDetailsDic = sameTableOrderDetailsDic;
            this.memoryCache = memoryCache;
            this.cuisineDbContext = cuisineDbContext;
            this.autoResetEventDic = autoResetEventDic;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderHistoryDetailById(string orderId)
        {
            Guid guid = new Guid(orderId);
            OrderDetail[] orderDetails = orderDbContext.OrderDetails.Where(orderDetail => orderDetail.OrderId == guid).ToArray();
            return Ok(orderDetails);
        }

        [HttpGet]
        public IActionResult GetOrderHistory(string session_key)
        {
            Console.WriteLine(session_key);
            string openid = redis_cache.GetString(session_key.Replace(" ", "+"));
            Console.Write("openid");
            Console.Write(openid);
            Order[] orders = orderDbContext.Orders.Where(order => order.OpenId == openid).OrderByDescending(order => order.DateTime).ToArray();

            foreach (var order in orders)
            {
                order.OpenId = null;
                Console.WriteLine(order.NickName);
            }
            return Ok(orders);

        }
        
        [HttpPost]
        public IActionResult AddOrderDetail(WXOrderDetailVo wXOrderDetailVo)
        {
            if (string.IsNullOrEmpty(wXOrderDetailVo.Session_key)) { return BadRequest("请先登录"); }
            string openId = redis_cache.GetString(wXOrderDetailVo.Session_key);
            if (string.IsNullOrEmpty(openId)) { return BadRequest("登录过期，请重新登录"); }

            //对openid加密，只返回加密后的openid给客户端
            byte[] md5_bytes = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(openId));
            string md5_openId = System.Text.Encoding.UTF8.GetString(md5_bytes);

            //使用内存缓存，减少查询数据库的频率
            List<Cuisine> cuisinesForCreateOrderDetail = memoryCache.GetOrCreate("cuisinesForCreateOrderDetail", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
                entry.SlidingExpiration=TimeSpan.FromHours(1);
                return cuisineDbContext.Cuisines.ToList();
            });
            Cuisine? cuisineToOrderDetail = cuisinesForCreateOrderDetail.SingleOrDefault(cuisine=>cuisine.Id == wXOrderDetailVo.Id);
            if (cuisineToOrderDetail == null)  return BadRequest("cuisine的Id不存在");
            string tableNo = wXOrderDetailVo.TableNo;
            List<WXOrderDetailDto>? wXOrderDetails;
            //点餐过程加锁，保持数据一致
            AutoResetEvent? autoResetEvent; 
            if (!autoResetEventDic.TryGetValue(wXOrderDetailVo.TableNo, out autoResetEvent)) {
                autoResetEventDic.Add(wXOrderDetailVo.TableNo,new AutoResetEvent(true));
            }
            bool isHaveLock = autoResetEventDic[tableNo].WaitOne(TimeSpan.FromSeconds(1));
            if (!isHaveLock)
            {
                
                return BadRequest("订单提交中,请勿点餐");
            }
            else
            {
                if (sameTableOrderDetailsDic.TryGetValue(tableNo, out wXOrderDetails))
                {//List<WXOrderDetailDto>存在，说明不是第一条点餐请求
                    int isListExsist = sameTableOrderDetailsDic[tableNo].FindIndex(orderDetail => orderDetail.Id == cuisineToOrderDetail.Id);
                    
                    if (isListExsist == -1)
                    {//List<WXOrderDetailDto>中不存在提交的orderDetaill，说明第一次添加该菜品
                        WXOrderDetailDto wXOrderDetailDto = new WXOrderDetailDto
                        {
                            Id = cuisineToOrderDetail.Id,
                            T_CuisineType_Id = cuisineToOrderDetail.T_CuisineType_Id,
                            OrderDetailName = cuisineToOrderDetail.CuisineName,
                            OrderDetailPrice = cuisineToOrderDetail.CuisinePrice,
                            OrderDetailPictureUrl = cuisineToOrderDetail.CuisinePictureUrl,
                            Number = 1,
                            Amount = cuisineToOrderDetail.CuisinePrice,
                            CustomerInfos = new List<CustomerInfo>()
                        };
                        wXOrderDetailDto.CustomerInfos.Add(new CustomerInfo
                        {
                            Nickname = wXOrderDetailVo.Nickname,
                            AvatarUrl = wXOrderDetailVo.AvatarUrl,
                            Number = 1,
                            MD5_OpenId = md5_openId

                        });
                        sameTableOrderDetailsDic[tableNo].Add(wXOrderDetailDto);
                    }
                    else
                    {//List<WXOrderDetailDto>中存在提交的orderDetaill，说明不是第一次添加菜品
                        WXOrderDetailDto wXOrderDetailDto = sameTableOrderDetailsDic[tableNo][isListExsist];
                        wXOrderDetailDto.Number += 1;
                        wXOrderDetailDto.Amount = wXOrderDetailDto.Number * wXOrderDetailDto.OrderDetailPrice;
                        int isCustomerExsist = wXOrderDetailDto.CustomerInfos.FindIndex(customerInfo => customerInfo.MD5_OpenId == md5_openId);
                        if (isCustomerExsist == -1)
                        {//orderDetail中不存在提交的顾客
                         //addOrSubOrderDetailVo.customerInfo.Number = 1;
                            CustomerInfo customerInfo = new CustomerInfo { Nickname = wXOrderDetailVo.Nickname, AvatarUrl = wXOrderDetailVo.AvatarUrl, Number = 1, MD5_OpenId = md5_openId };
                            wXOrderDetailDto.CustomerInfos.Add(customerInfo);
                        }
                        else
                        {//orderDetail中存在提交的顾客
                            if (wXOrderDetailDto.CustomerInfos[isCustomerExsist].Nickname != wXOrderDetailVo.Nickname || wXOrderDetailDto.CustomerInfos[isCustomerExsist].AvatarUrl != wXOrderDetailVo.AvatarUrl)
                            {//顾客点餐后，修改了自己的昵称或头像
                                wXOrderDetailDto.CustomerInfos[isCustomerExsist].Nickname = wXOrderDetailVo.Nickname;
                                wXOrderDetailDto.CustomerInfos[isCustomerExsist].AvatarUrl = wXOrderDetailVo.AvatarUrl;
                            }
                            wXOrderDetailDto.CustomerInfos[isCustomerExsist].Number += 1;
                        }
                        sameTableOrderDetailsDic[tableNo][isListExsist] = wXOrderDetailDto;
                    }
                }
                else
                { //List<WXOrderDetailDto>不存在，第一条点餐请求
                    WXOrderDetailDto wXOrderDetailDto = new WXOrderDetailDto
                    {
                        Id = cuisineToOrderDetail.Id,
                        T_CuisineType_Id = cuisineToOrderDetail.T_CuisineType_Id,
                        OrderDetailName = cuisineToOrderDetail.CuisineName,
                        OrderDetailPrice = cuisineToOrderDetail.CuisinePrice,
                        OrderDetailPictureUrl = cuisineToOrderDetail.CuisinePictureUrl,
                        Number = 1,
                        Amount = cuisineToOrderDetail.CuisinePrice,
                        CustomerInfos = new List<CustomerInfo>()
                    };
                    wXOrderDetailDto.CustomerInfos.Add(new CustomerInfo
                    {
                        Nickname = wXOrderDetailVo.Nickname,
                        AvatarUrl = wXOrderDetailVo.AvatarUrl,
                        Number = 1,
                        MD5_OpenId = md5_openId
                    });
                    List<WXOrderDetailDto> wXOrderDetailDtos = new List<WXOrderDetailDto> { wXOrderDetailDto };
                    sameTableOrderDetailsDic.Add(tableNo, wXOrderDetailDtos);
                }
                autoResetEventDic[tableNo].Set();
            }
            return Ok(sameTableOrderDetailsDic);
        }

        [HttpPost]
        public IActionResult SubOrderDetail(WXOrderDetailVo wXOrderDetailVo)
        {
            if (string.IsNullOrEmpty(wXOrderDetailVo.Session_key)) { return BadRequest("请先登录"); }
            string openId = redis_cache.GetString(wXOrderDetailVo.Session_key);
            if (string.IsNullOrEmpty(openId)) { return BadRequest("登录过期，请重新登录"); }

            byte[] md5_bytes = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(openId));
            string md5_openId = System.Text.Encoding.UTF8.GetString(md5_bytes);

            List<Cuisine> cuisinesForCreateOrderDetail = memoryCache.GetOrCreate("cuisinesForCreateOrderDetail", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                return cuisineDbContext.Cuisines.ToList();
            });
            Cuisine? cuisineToOrderDetail = cuisinesForCreateOrderDetail.SingleOrDefault(cuisine => cuisine.Id == wXOrderDetailVo.Id);
            if (cuisineToOrderDetail == null) return BadRequest("cuisine的Id不存在");

            string tableNo = wXOrderDetailVo.TableNo;
            List<WXOrderDetailDto>? wXOrderDetails;

            if (!sameTableOrderDetailsDic.TryGetValue(tableNo, out wXOrderDetails)) { return BadRequest("wXOrderDetailDtoList不存在,请先扫码"); }
            
            AutoResetEvent? autoResetEventOfTablle;
            if (!autoResetEventDic.TryGetValue(tableNo, out autoResetEventOfTablle)) { return BadRequest("请先点餐"); }

            bool isHaveLock = autoResetEventDic[tableNo].WaitOne(TimeSpan.FromSeconds(1));
            if (!isHaveLock) { return BadRequest("订单提交中，请勿点餐"); }
            //List<WXOrderDetailDto>存在
            int indexOfOrderDetail = wXOrderDetails.FindIndex(wXOrderDetail => wXOrderDetail.Id == wXOrderDetailVo.Id);
            if (indexOfOrderDetail <= -1) { autoResetEventDic[tableNo].Set(); return BadRequest("orderDetail不存在"); }
            WXOrderDetailDto wXOrderDetailDto = sameTableOrderDetailsDic[tableNo][indexOfOrderDetail];
            if (wXOrderDetailDto.Number <= 0) { autoResetEventDic[tableNo].Set(); return BadRequest("orderDetail数量<=0时不能减少"); }
            wXOrderDetailDto.Number -= 1;
            if (wXOrderDetailDto.Number >= 0)
            {
                wXOrderDetailDto.Amount -= wXOrderDetailDto.OrderDetailPrice;
                int indexOfCustomerInfo = wXOrderDetailDto.CustomerInfos.FindIndex(customerInfo => customerInfo.MD5_OpenId == md5_openId);
                if (indexOfCustomerInfo <= -1) { autoResetEventDic[tableNo].Set(); return BadRequest("账号信息错误，请先登录"); }
                //顾客点餐后，修改了自己的昵称或头像
                if (wXOrderDetailDto.CustomerInfos[indexOfCustomerInfo].Nickname != wXOrderDetailVo.Nickname || wXOrderDetailDto.CustomerInfos[indexOfCustomerInfo].AvatarUrl != wXOrderDetailVo.AvatarUrl)
                {
                    wXOrderDetailDto.CustomerInfos[indexOfCustomerInfo].Nickname = wXOrderDetailVo.Nickname;
                    wXOrderDetailDto.CustomerInfos[indexOfCustomerInfo].AvatarUrl = wXOrderDetailVo.AvatarUrl;
                }
                wXOrderDetailDto.CustomerInfos[indexOfCustomerInfo].Number -= 1;
                sameTableOrderDetailsDic[tableNo][indexOfOrderDetail] = wXOrderDetailDto;
            }
            else
            {//orderDetail数量减1后小于0，移除该orderDetail
                sameTableOrderDetailsDic[tableNo].RemoveAt(indexOfOrderDetail);
            }
            autoResetEventDic[tableNo].Set();
            return Ok(sameTableOrderDetailsDic);
        }
        
        [HttpGet]
        public IActionResult GetSameTableOrderDetails(string tableNo)
        {
            List<WXOrderDetailDto>? wXOrderDetailDtos = null;
            sameTableOrderDetailsDic.TryGetValue(tableNo, out wXOrderDetailDtos);
            return Ok(wXOrderDetailDtos);
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
        public async Task<IActionResult> SubmitOrder(WxOrderVo wxOrderVo)
        {
            if (string.IsNullOrEmpty(wxOrderVo.Session_key)) { return BadRequest("请先登录"); }
            string openId = redis_cache.GetString(wxOrderVo.Session_key);
            if (string.IsNullOrEmpty(openId)) { return BadRequest("登录过期，请重新登录"); }
            if (string.IsNullOrEmpty(wxOrderVo.TableNo)) { return BadRequest("请先扫码"); }

            List<WXOrderDetailDto>? wXOrderDetailDtos;
            _ = sameTableOrderDetailsDic.TryGetValue(wxOrderVo.TableNo, out wXOrderDetailDtos);
            if (wXOrderDetailDtos == null) { return BadRequest("请先点餐"); }
            Order order = new Order { DateTime = DateTime.Now, TableNo = wxOrderVo.TableNo, OpenId = openId,NickName = wxOrderVo.Nickname};
            order.Total = 0;
            order.PayState = 0;
            List<OrderDetail> orderDetailList = new List<OrderDetail>();
            using (TransactionScope tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    wXOrderDetailDtos.ForEach(orderDetailDto => {
                        OrderDetail orderDetail = new OrderDetail();
                        orderDetail.CuisineName = orderDetailDto.OrderDetailName;
                        orderDetail.CuisinePrice = orderDetailDto.OrderDetailPrice;
                        orderDetail.Number = orderDetailDto.Number;
                        orderDetail.Amout = orderDetailDto.Amount;
                        order.Total += orderDetailDto.Amount;
                        orderDbContext.OrderDetails.Add(orderDetail);
                        orderDetailList.Add(orderDetail);
                    });
                    order.OrderDetails = orderDetailList;
                    orderDbContext.Orders.Add(order);
                    orderDbContext.SaveChanges();
                    tx.Complete();
                    return Ok(wXOrderDetailDtos);
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
