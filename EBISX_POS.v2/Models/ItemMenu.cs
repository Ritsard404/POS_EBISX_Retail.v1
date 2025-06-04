using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.IO;

namespace EBISX_POS.Models
{
    public class ItemMenu
    {
        public int Id { get; set; }
        public required string ItemName { get; set; }
        public required decimal Price { get; set; }
        public string? Size { get; set; }
        public bool HasSize { get; set; }
        public bool HasAddOn { get; set; }
        public bool HasDrink { get; set; }
        public bool IsSolo { get; set; }
        public bool  IsAddOn { get; set; }
        public bool  IsDrink { get; set; }
        public bool  IsVatZero { get; set; }

        private string? _imagePath;
        public string? ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                ItemImage = string.IsNullOrEmpty(value) ? null : LoadBitmap(value);
            }
        }

        public Bitmap? ItemImage { get; private set; }

        private Bitmap? LoadBitmap(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                var uri = new Uri(path);

                // If the URI is a file, load it from disk
                if (uri.IsFile)
                {
                    using var stream = File.OpenRead(path);
                    return new Bitmap(stream);
                }
                else
                {
                    // Otherwise, assume it's an asset URI
                    var assets = AssetLoader.Open(uri);
                    return new Bitmap(assets);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
