using Microsoft.EntityFrameworkCore;

namespace NSprob.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<User>          Users          => Set<User>();
        public DbSet<PendingVerify> PendingVerifies => Set<PendingVerify>();
        public DbSet<Conversation>  Conversations  => Set<Conversation>();
        public DbSet<Message>       Messages       => Set<Message>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
            mb.Entity<User>().HasIndex(u => u.Username).IsUnique();
        }
    }

    public class User
    {
        public string   Id           { get; set; } = Guid.NewGuid().ToString();
        public string   Username     { get; set; } = "";
        public string   Email        { get; set; } = "";
        public string   PasswordHash { get; set; } = "";
        public string   PublicKey    { get; set; } = ""; // RSA-2048 публічний ключ
        public bool     IsVerified   { get; set; }
        public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    }

    public class PendingVerify
    {
        public int      Id        { get; set; }
        public string   Email     { get; set; } = "";
        public string   Username  { get; set; } = "";
        public string   PublicKey { get; set; } = "";
        public string   Code      { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }

    public class Conversation
    {
        public string   Id           { get; set; } = Guid.NewGuid().ToString();
        public string   User1Id      { get; set; } = "";
        public string   User2Id      { get; set; } = "";
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }

    public class Message
    {
        public string   Id               { get; set; } = Guid.NewGuid().ToString();
        public string   ConversationId   { get; set; } = "";
        public string   SenderId         { get; set; } = "";
        public string   EncryptedContent { get; set; } = "";
        public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
    }
}
