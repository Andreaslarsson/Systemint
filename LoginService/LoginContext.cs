using LoginService.Model;
using Microsoft.EntityFrameworkCore;

namespace LoginService
{
    public class LoginContext : DbContext
    {
        public LoginContext(DbContextOptions<LoginContext> options) : base(options) {}
        public DbSet<User> Users => Set<User>();
    }
}
