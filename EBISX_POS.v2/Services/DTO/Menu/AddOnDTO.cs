using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;

namespace EBISX_POS.Services.DTO.Menu
{
    /// <summary>
    /// Represents a collection of add-on categories and their associated items
    /// </summary>
    public class AddOnDTO
    {
        /// <summary>
        /// List of add-on categories (e.g., Sauces, Toppings) with their items
        /// </summary>
        public List<AddOnTypeDTO> AddOnTypesWithAddOns { get; set; }
    }

    /// <summary>
    /// Represents a category of add-on items
    /// </summary>
    public class AddOnTypeDTO
    {
        /// <summary>
        /// Unique identifier for the add-on category
        /// </summary>
        public int AddOnTypeId { get; set; }

        /// <summary>
        /// Display name for the add-on category
        /// </summary>
        public string AddOnTypeName { get; set; }

        /// <summary>
        /// List of individual add-on items in this category
        /// </summary>
        public List<AddOnDetailDTO> AddOns { get; set; }
    }

    /// <summary>
    /// Represents an individual add-on item
    /// </summary>
    public class AddOnDetailDTO
    {
        public int MenuId { get; set; }

        /// <summary>
        /// Name of the add-on item
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// Optional size specification for the add-on
        /// </summary>
        public string? Size { get; set; }
        public bool HasSize { get; set; } = false;

        /// <summary>
        /// Price of the add-on item
        /// </summary>
        public decimal Price { get; set; }
        public bool IsUpgradeMeal { get; set; }

        private string? _menuImagePath;
        public Bitmap? ItemImage { get; private set; }

        public string? MenuImagePath
        {
            get => _menuImagePath;
            set
            {
                _menuImagePath = value;
                ItemImage = string.IsNullOrEmpty(value) ? null : LoadBitmap(value);
            }
        }
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
