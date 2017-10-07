using System;
using System.Windows.Media.Imaging;
using BITS.DiscoveryService;
using System.Drawing.Imaging;
using System.IO;

namespace ProgettoApp
{
    class ImageCaption
    {
        private String _Caption;
        private BitmapImage _Image;
        private String _HashButton;
        /// <summary>
        /// Image with username as caption
        /// </summary>
        /// <param name="u">Username to render</param>
        public ImageCaption(User u)
        {
            using (var ms = new MemoryStream())
            {
                System.Drawing.Image i = (System.Drawing.Image)u.Userimage.Clone();
                i.Save(ms, ImageFormat.Jpeg);
                this.Image = new BitmapImage();
                this.Image.BeginInit();
                this.Image.StreamSource = new MemoryStream(ms.ToArray());
                this.Image.EndInit();
                i.Dispose();
            }
            this.HashButton = u.GetHashCode().ToString();
            this.Caption = u.Username;
        }

        public string Caption { get => _Caption; set => _Caption = value; }
        public BitmapImage Image { get => _Image; set => _Image = value; }
        public string HashButton { get => _HashButton; set => _HashButton = value; }
    }
}
