using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NSprob.Server.Data;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace NSprob.Server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // userId → connectionId (в пам'яті, для продакшн — Redis)
        private static readonly ConcurrentDictionary<string, string> _online = new();

        private readonly AppDbContext _db;
        public ChatHub(AppDbContext db) => _db = db;

        private string MyId => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        public override Task OnConnectedAsync()
        {
            _online[MyId] = Context.ConnectionId;
            Console.WriteLine($"  [↑] Connected: {Context.User?.Identity?.Name} ({MyId[..8]}…)");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? ex)
        {
            _online.TryRemove(MyId, out _);
            Console.WriteLine($"  [↓] Disconnected: {Context.User?.Identity?.Name}");
            return base.OnDisconnectedAsync(ex);
        }

        // Клієнт викликає: SendMessage(recipientId, encryptedContent)
        public async Task SendMessage(string recipientId, string encryptedContent)
        {
            // Знайди або створи conversation
            var convo = await _db.Conversations.FirstOrDefaultAsync(c =>
                (c.User1Id == MyId && c.User2Id == recipientId) ||
                (c.User1Id == recipientId && c.User2Id == MyId));

            if (convo == null)
            {
                convo = new Conversation { User1Id = MyId, User2Id = recipientId };
                _db.Conversations.Add(convo);
            }
            convo.LastActivity = DateTime.UtcNow;

            // Збережи повідомлення
            var msg = new Message
            {
                ConversationId   = convo.Id,
                SenderId         = MyId,
                EncryptedContent = encryptedContent,
                CreatedAt        = DateTime.UtcNow
            };
            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();

            // Доставка одержувачу якщо онлайн
            if (_online.TryGetValue(recipientId, out var connId))
            {
                var sender = await _db.Users.FindAsync(MyId);
                await Clients.Client(connId).SendAsync("ReceiveMessage", new
                {
                    id               = msg.Id,
                    senderId         = MyId,
                    senderUsername   = sender?.Username ?? "",
                    encryptedContent = msg.EncryptedContent,
                    isOutgoing       = false,
                    timeDisplay      = msg.CreatedAt.ToLocalTime().ToString("HH:mm")
                });
            }

            Console.WriteLine($"  [✉] {Context.User?.Identity?.Name} → {recipientId[..8]}…");
        }

        // Клієнт викликає: NotifyTyping(recipientId)
        public async Task NotifyTyping(string recipientId)
        {
            if (_online.TryGetValue(recipientId, out var connId))
                await Clients.Client(connId).SendAsync("UserTyping", MyId);
        }
    }
}
