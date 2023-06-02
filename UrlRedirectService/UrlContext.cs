using Model;
using Microsoft.EntityFrameworkCore;

public class UrlContext : DbContext
{
    public UrlContext(DbContextOptions<UrlContext> options) : base(options) { }

    public DbSet<URL> URLs => Set<URL>();
}

