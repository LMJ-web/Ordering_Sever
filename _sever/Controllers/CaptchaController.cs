using _sever.entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Collections;

namespace _sever.Controllers
{
    [ApiController]
    [Route("[Controller]/[Action]")]
    public class CaptchaController: ControllerBase
    {
        private readonly IDistributedCache _cache;
        public CaptchaController(IDistributedCache cache)
        {
            _cache = cache;
        }
        [HttpGet]
        public ActionResult GetCaptchaImage() {
            Bitmap image = new Bitmap(300, 100);
            //画板
            Graphics g = Graphics.FromImage(image);

            Rectangle rectangle = new Rectangle(0, 0, image.Width, image.Height);
            Image TextTureImage = Image.FromFile("./StaticDataSource/TextTure.jpg");
            //底纹画刷
            TextureBrush textureBrush = new TextureBrush(TextTureImage);
            //在画板上使用画刷，填充矩形
            g.FillRectangle(textureBrush, rectangle);
            //字体、大小、样式
            Font font = new Font("Georgia", 30, (FontStyle.Bold | FontStyle.Italic));   
            //在画板上用画刷，写字
            LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(image.Width, image.Height), Color.Blue, Color.DarkRed);

            ArrayList codeList = GenerateCode();
            int x = 40;
            foreach (string code in codeList)
            {
                g.DrawString(code, font, brush, x, 30);
                x += 60;
            }

            //点
            Point p1 = new Point(0, 70);
            Point p2 = new Point(300, 60);
            //设置笔的颜色和宽度
            Pen pen = new Pen(Color.Green, 2);

            Point p3 = new Point(0, 50);
            Point p4 = new Point(300, 60);
            Pen pen1 = new Pen(Color.Gray, 2);
            //在画板使用笔，画线
            g.DrawLine(pen, p1, p2);
            g.DrawLine(pen1, p3, p4);

            MemoryStream stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);

            //销毁画板和笔
            g.Dispose();
            pen.Dispose();

            string codeString = "";
            foreach (string code in codeList)
            {
                codeString += code;
            }
            var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(30));
            //往redis中存验证码
            _cache.Set(HttpContext.Connection.Id, Encoding.UTF8.GetBytes(codeString), options);
            return Ok(stream.ToArray());
        }
        private ArrayList GenerateCode()
        {
            ArrayList code = new ArrayList();
            string[] chars = { 
                "A","B","C","D","E","F","G","H","I","J","K","L","M","N","P","Q","R","S","T","U","V","W","X","Y","Z",
                "a","b","c","d","e","f","g","h","i","j","k","l","m","n","p","q","r","s","t","u","v","w","x","y","z",
                "1","2","3","4","5","6","7","8","9"
            };
            Random random = new Random();
            for (int i = 0; i <4; i++) { 
                int j = random.Next(0,chars.Length);
                code.Add(chars[j]);
            }
            return code;
        }

    }
}
