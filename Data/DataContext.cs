using MessagingApp.Models;
using Microsoft.EntityFrameworkCore;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<RefreshToken> RefreshTokens { set; get; }
    public DbSet<RevokedJWTs> revokedJWTs { set; get; }
    public DbSet<Chat> Chats { set; get; }

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

                modelBuilder.Entity<Message>()
                .HasMany(m => m.readByUsers)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "MessageReadUsers", // Join table name
                    j => j.HasOne<User>().WithMany().HasForeignKey("UserId"),
                    j => j.HasOne<Message>().WithMany().HasForeignKey("MessageId")
        );

            msg.HasOne(m => m.OriginUser)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.OriginID)
            .OnDelete(DeleteBehavior.Restrict);

            msg.HasIndex(m => new { m.ChatID, m.Timestamp })
            .HasDatabaseName("IX_Messages_ChatId_CreatedAt_Desc")
            .IsDescending([false, true]);
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

        //TORTURE M:M




        modelBuilder.Entity<User>(user =>
        {
            user.HasMany(u => u.enrolledChats)
            .WithMany(cht => cht.Members)
            .UsingEntity<Dictionary<string, string>>(
                "UserChatJoinTable",
                tbl =>
                    tbl.HasOne<Chat>()
                    .WithMany()
                    .HasForeignKey("ChatID")
                    .HasConstraintName("FK_UserChatJoinTable_Chats_ChatID"),
                tbl =>
                    tbl.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserID")
                    .HasConstraintName("FK_UserChatJoinTable_Users_UserID"),
                tbl =>
                {
                    tbl.HasKey("ChatID", "UserID");
                    tbl.ToTable("UserChatJoinTable");
                }
            );
        });

        modelBuilder.Entity<RevokedJWTs>().ToTable("revoked_jwts");

        modelBuilder.Entity<Chat>(chat =>
        {
            chat.HasKey(cht => cht.ID);

            chat.HasMany<Message>(cht => cht.Messages)
            .WithOne()
            .HasForeignKey(msg => msg.ChatID)
            .OnDelete(DeleteBehavior.Cascade);

            chat.HasOne(c => c.Owner)
            .WithMany(u => u.OwnedChats)
            .HasForeignKey(c => c.OwnerID)
            .HasConstraintName("FK_OwnerChat")
            .OnDelete(DeleteBehavior.Restrict);
        });
    }
}