using _sever.EF_Core.OrderAndDetail;

namespace _sever.entity
{
    public class WxOrderVo
    {
       
        public string TableNo { get; set; }
        public string Nickname { get; set; }
        public string? Session_key { get; set; }
    }
}
