using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HRMS.utils
{
    class ImageUtil
    {

        public static BitmapImage loadImage(byte[] rawImage)
        {
            if (rawImage == null) return null;
            using (var ms = new System.IO.MemoryStream(rawImage))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; // here
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }
    }
}
