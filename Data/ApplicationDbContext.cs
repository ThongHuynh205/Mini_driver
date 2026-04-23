// DbContext
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