using Avalonia.Media.Imaging;
using Avalonia.Platform;
using EBISX_POS.API.Services.DTO.Menu;
using System;
using System.Collections.Generic;
using System.IO;

namespace EBISX_POS.Services.DTO.Menu
{
    /// <summary>
    /// Represents drink options and available sizes for a menu item
    /// </summary>
    public class DrinksDTO
    {
        /// <summary>
        /// Categorized drink types (e.g., Sodas, Juices) with their options
        /// </summary>
        public List<DrinkTypeDTO> DrinkTypesWithDrinks { get; set; }

        /// <summary>
        /// Available size options for drinks
        /// </summary>
        public List<string> Sizes { get; set; }
    }

    /// <summary>
    /// Represents a category of drinks
    /// </summary>
    public class DrinkTypeDTO
    {
        /// <summary>
        /// Unique identifier for the drink category
        /// </summary>
        public int DrinkTypeId { get; set; }

        /// <summary>
        /// Display name for the drink category
        /// </summary>
        public string DrinkTypeName { get; set; }

        /// <summary>
        /// List of drink options in this category
        /// </summary>
        public List<SizesWithPricesDTO>? SizesWithPrices { get; set; } = new List<SizesWithPricesDTO>();
    }

    /// <summary>
    /// Represents size options with their associated drinks
    /// </summary>
    public class SizesWithPricesDTO
    {
        /// <summary>
        /// The size identifier (e.g., "R", "L")
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// List of drinks available in this size
        /// </summary>
        public List<DrinkDetailDTO>? Drinks { get; set; }
    }

    /// <summary>
    /// Represents an individual drink option
    /// </summary>
    public class DrinkDetailDTO
    {
        public int MenuId { get; set; }
        public string MenuName { get; set; }

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
        public decimal MenuPrice { get; set; }
        public string? Size { get; set; }
        public bool IsUpgradeMeal { get; set; }
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
