using System;

namespace Mini_driver.Server.Models
{
    public class FileMetadata
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }

        public FileMetadata(string fileName, long fileSize, DateTime uploadDate)
        {
            FileName = fileName;
            FileSize = fileSize;
            UploadDate = uploadDate;
        }
    }
}