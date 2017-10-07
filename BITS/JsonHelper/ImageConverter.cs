using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;

namespace BITS.JsonHelper
{
    class ImageConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            String base64String = (String)reader.Value;
            // convert base64 to byte array, put that into memory stream and feed to image
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(base64String))) {
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (BufferedStream bf = new BufferedStream(gz))
                    {
                        return Image.FromStream(gz);
                    }
                }
            }                
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Image image = (Image)((Image)value).Clone();
            // save to memory stream in original format
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(ms, CompressionLevel.Optimal))
                {
                    using (BufferedStream bf = new BufferedStream(gz))
                    {
                        image.Save(bf, ImageFormat.Jpeg);
                        bf.Flush();
                        gz.Flush();
                        gz.Close();
                        Byte[] imageBytes = ms.ToArray();

                        String base64String = Convert.ToBase64String(imageBytes);
                        // write byte array, will be converted to base64 by JSON.NET
                        writer.WriteValue(base64String);
                    }
                }
            }
            image.Dispose();
        }
        
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Image);
        }

        /* OLD implementation
        /// <summary>
        /// Compare2Image redefine the comparing way for 2 Image 
        /// by comparing it with a boolean flag (black/white).
        /// We use this approach because the scaled image 
        /// (sended on network as jpeg compressed) is not equal in 
        /// quality to the orignal one.
        /// </summary>
        /// <param name="first">First Image</param>
        /// <param name="second">Second Image</param>
        /// <returns>bool that stat for equality/inequality</returns>
        public static Boolean Compare2Image(Image first,Image second)
        {
            Bitmap firstRed = new Bitmap(first, new Size(16,16));
            Bitmap secondRed = new Bitmap(first, new Size(16, 16));
            Boolean equals = true;
            for (int j = 0; j < firstRed.Height && equals; j++)
            {
                for (int i = 0; i < firstRed.Width && equals; i++)
                {
                    equals=((firstRed.GetPixel(i, j).GetBrightness()<0.5f) == (secondRed.GetPixel(i, j).GetBrightness()<0.5f));
                }
            }
            return equals;
        }
        /// <summary>
        /// HashImager redefine the way to hash an image
        /// caluculating on it by nuber of white pixel of scaled image
        /// We use this approach because the scaled image 
        /// (sended on network as jpeg compressed) is not equal in 
        /// quality to the orignal one.
        /// </summary>
        /// <param name="first"></param>
        /// <returns></returns>
        public static int HashImage(Image first)
        {
            Bitmap firstRed = new Bitmap(first, new Size(16, 16));
            int hash = 0;
            for (int j = 0; j < firstRed.Height; j++)
            {
                for (int i = 0; i < firstRed.Width; i++)
                {
                    hash += Convert.ToInt32(firstRed.GetPixel(i, j).GetBrightness()<0.5f);
                }
            }
            return hash;
        }
        */
    }
}