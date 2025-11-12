using Microsoft.EntityFrameworkCore;
using TodoApi.Core;

namespace TodoApi.Infrastructure

{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options) { }

        public DbSet<TodoItem> TodoItems { get; set; }
    }
}