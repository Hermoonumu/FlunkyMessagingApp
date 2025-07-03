using MessagingApp.Models;
using Microsoft.EntityFrameworkCore;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<RefreshToken> RefreshTokens { set; get; }
    public DbSet<RevokedJWTs> revokedJWTs { set; get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(user => user.ID);

            user.HasAlternateKey(u => u.Username);
        });


        modelBuilder.Entity<Message>(msg =>
        {
            msg.HasKey(m => m.ID);

            msg.HasOne(m => m.DestinationUser)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.DestinationID)
            .OnDelete(DeleteBehavior.Restrict);

            msg.HasOne(m => m.OriginUser)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.OriginID)
            .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(
            token =>
            {
                token.HasOne(u => u.user)
                .WithMany()
                .HasForeignKey(tk => tk.UserID)
                .OnDelete(DeleteBehavior.Restrict);

                token.HasKey(tk => tk.ID);
            }
        );

        modelBuilder.Entity<RevokedJWTs>(token =>
        {
            token.HasKey(tk => tk.ID);
        });
    }
}