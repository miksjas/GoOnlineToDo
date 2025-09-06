using GoOnlineToDo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoOnlineToDo.Infrastructure.Data;

public class ToDoDbContext : DbContext
{
    public ToDoDbContext(DbContextOptions<ToDoDbContext> options) : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PercentComplete).HasDefaultValue(0);
            entity.Property(t => t.DueDate).HasColumnType("timestamp without time zone");
        });
    }
}