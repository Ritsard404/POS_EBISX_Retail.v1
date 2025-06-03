using System;
using System.IO;

namespace EBISX_POS.Settings
{
    public class FilePaths
    {
        private string _imagePath = string.Empty;
        private string _backUp = string.Empty;

        public required string ImagePath 
        { 
            get => _imagePath;
            set => _imagePath = GetFullPath(value);
        }
        
        public required string BackUp 
        { 
            get => _backUp;
            set => _backUp = GetFullPath(value);
        }

        public static FilePaths CreateDefault()
        {
            return new FilePaths
            {
                ImagePath = "Images",
                BackUp = "Backups"
            };
        }

        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(ImagePath);
            Directory.CreateDirectory(BackUp);
        }

        private string GetFullPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // If it's an absolute path, return it as is
            if (Path.IsPathRooted(path))
                return path;

            // Otherwise, make it relative to the application's base directory
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        }
    }
} 