// FileItem model
namespace Mini_driver.Models
{
    public class FileItem
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploaderName { get; set; }
    }
}