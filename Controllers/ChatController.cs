using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSprob.Server.Data;
using System.Security.Claims;

namespace NSprob.Server.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ChatController(AppDbContext db) => _db = db;

        private string MyId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        // GET /api/chat/contacts
        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var convos = await _db.Conversations
                .Where(c => c.User1Id == MyId || c.User2Id == MyId)
                .OrderByDescending(c => c.LastActivity)
                .ToListAsync();

            var result = new List<object>();
            foreach (var c in convos)
            {
                var otherId = c.User1Id == MyId ? c.User2Id : c.User1Id;
                var other   = await _db.Users.FindAsync(otherId);
                if (other == null) continue;

                var last = await _db.Messages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                result.Add(new
                {
                    userId    = other.Id,
                    username  = other.Username,
                    publicKey = other.PublicKey,
                    lastMsg   = "",  // encrypted, не показуємо
                    lastTime  = last?.CreatedAt.ToLocalTime().ToString("HH:mm") ?? "",
                    unread    = 0
                });
            }
            return Ok(result);
        }

        // GET /api/chat/history/{contactId}
        [HttpGet("history/{contactId}")]
        public async Task<IActionResult> GetHistory(string contactId)
        {
            var convo = await _db.Conversations.FirstOrDefaultAsync(c =>
                (c.User1Id == MyId && c.User2Id == contactId) ||
                (c.User1Id == contactId && c.User2Id == MyId));

            if (convo == null) return Ok(Array.Empty<object>());

            var msgs = await _db.Messages
                .Where(m => m.ConversationId == convo.Id)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .ToListAsync();

            return Ok(msgs.Select(m => new
            {
                id               = m.Id,
                senderId         = m.SenderId,
                encryptedContent = m.EncryptedContent,
                isOutgoing       = m.SenderId == MyId,
                timeDisplay      = m.CreatedAt.ToLocalTime().ToString("HH:mm")
            }));
        }

        // POST /api/chat/start
        [HttpPost("start")]
        public async Task<IActionResult> StartChat([FromBody] StartReq req)
        {
            var other = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (other == null) return NotFound("User not found.");
            if (other.Id == MyId) return BadRequest("Cannot chat with yourself.");

            var existing = await _db.Conversations.FirstOrDefaultAsync(c =>
                (c.User1Id == MyId && c.User2Id == other.Id) ||
                (c.User1Id == other.Id && c.User2Id == MyId));

            if (existing == null)
            {
                _db.Conversations.Add(new Conversation { User1Id = MyId, User2Id = other.Id });
                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                userId   = other.Id,
                username = other.Username,
                publicKey = other.PublicKey,
                lastMsg  = "",
                lastTime = "",
                unread   = 0
            });
        }
    }

    public record StartReq(string Username);
}
