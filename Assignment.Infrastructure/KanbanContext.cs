namespace Assignment.Infrastructure;

public class KanbanContext : DbContext
{
    public KanbanContext(DbContextOptions<KanbanContext> options) : base(options)
    {
    }

    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Task> Tasks => Set<Task>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Making the enum to string
        modelBuilder
            .Entity<Task>()
            .Property(e => e.State)
            .HasConversion(
                s => s.ToString(),
                s => (State)Enum.Parse(typeof(State), s));

        //Making title required and setting length to be max 100
        modelBuilder.Entity<Task>()
            .Property(t => t.Title)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Tag>()
            .Property(t => t.Name)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();
    }
}