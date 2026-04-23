// FileService
using Mini_driver.Models;

namespace Mini_driver.Services
{
    public class FileService
    {
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

        public FileService()
        {
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            var filePath = Path.Combine(_storagePath, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return filePath;
        }

        public string GetFilePath(string fileName) => Path.Combine(_storagePath, fileName);
    }
}