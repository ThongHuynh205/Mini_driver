using System;

namespace Mini_driver.Client.Models
{
    public class FileMetadata
    {
        // ID định danh file trong Database
        public int FileID { get; set; }

        // Tên tệp tin (ví dụ: "tailieu.pdf")
        public string FileName { get; set; } = string.Empty;

        // Kích thước tệp (tính bằng Byte)
        public long FileSize { get; set; }

        // Ngày giờ tải lên
        public DateTime UploadDate { get; set; }

        // ID của người sở hữu (Foreign Key liên kết với bảng Users)
        public int OwnerID { get; set; }

        // Đường dẫn vật lý đầy đủ trên ổ cứng Server
        public string LocalPath { get; set; } = string.Empty;
    }
}