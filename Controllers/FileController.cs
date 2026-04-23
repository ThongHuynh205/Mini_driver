using Microsoft.AspNetCore.Mvc;
using Mini_driver.Data;
using Mini_driver.Models;
using Mini_driver.Services;

namespace Mini_driver.Controllers
{
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly FileService _fileService;

        public FileController(ApplicationDbContext db, FileService fileService)
        {
            _db = db;
            _fileService = fileService;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return Content("File không hợp lệ");

            var storedName = await _fileService.SaveFileAsync(file);

            var fileEntry = new FileItem
            {
                FileName = file.FileName, // Tên gốc
                FilePath = storedName,    // Tên trên server
                FileSize = file.Length
            };

            _db.FileItems.Add(fileEntry);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Download(int id)
        {
            var file = _db.FileItems.Find(id);
            if (file == null) return NotFound();

            var path = _fileService.GetPhysicalPath(file.FilePath);
            var fileBytes = System.IO.File.ReadAllBytes(path);

            return File(fileBytes, "application/octet-stream", file.FileName);
        }
    }
}   