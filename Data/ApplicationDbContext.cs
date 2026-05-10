// DbContext
<<<<<<< HEAD
using Microsoft.EntityFrameworkCore;
using Mini_driver.Models;

namespace Mini_driver.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<FileItem> FileItems { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
=======
>>>>>>> 13726f536716ce0f043fd99701b46665d3b0048a
