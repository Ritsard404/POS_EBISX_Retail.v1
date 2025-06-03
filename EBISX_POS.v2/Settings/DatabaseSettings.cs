using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace EBISX_POS.Settings
{
    public class DatabaseSettings
    {
        private string _posConnectionString = "Data Source=ebisx_pos.db";
        private string _journalConnectionString = "Data Source=ebisx_journal.db";
        private string _backupDirectory = "Backups";

        [Required]
        public string PosConnectionString 
        { 
            get => _posConnectionString;
            set => _posConnectionString = GetAbsolutePath(value);
        }

        [Required]
        public string JournalConnectionString 
        { 
            get => _journalConnectionString;
            set => _journalConnectionString = GetAbsolutePath(value);
        }

        public bool EnableDetailedErrors { get; set; } = false;
        
        public bool EnableSensitiveDataLogging { get; set; } = false;
        
        public int CommandTimeout { get; set; } = 30;
        
        public string BackupDirectory 
        { 
            get => _backupDirectory;
            set => _backupDirectory = GetAbsolutePath(value);
        }
        
        public bool EnableAutomaticBackup { get; set; } = true;
        
        public int BackupRetentionDays { get; set; } = 7;

        private string GetAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // If it's a connection string, extract the path
            if (path.StartsWith("Data Source="))
            {
                var dbPath = path.Replace("Data Source=", "");
                // If it's an absolute path, return it as is
                if (Path.IsPathRooted(dbPath))
                    return path;
                // Otherwise, make it relative to the application's base directory
                var absolutePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dbPath));
                return $"Data Source={absolutePath}";
            }

            // For regular paths
            if (Path.IsPathRooted(path))
                return path;
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        }
    }
}