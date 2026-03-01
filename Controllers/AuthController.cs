using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSprob.Server.Data;
using NSprob.Server.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace NSprob.Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokens;
        private readonly EmailService _email;

        public AuthController(AppDbContext db, TokenService tokens, EmailService email)
        { _db = db; _tokens = tokens; _email = email; }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.PublicKey))
                return BadRequest("All fields required.");
            var emailLower = req.Email.ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Email == emailLower || u.Username == req.Username))
                return Conflict("Username or email already taken.");
            _db.PendingVerifies.RemoveRange(_db.PendingVerifies.Where(p => p.Email == emailLower));
            var code = RandomNumberGenerator.GetInt32(100_000, 999_999).ToString();
            _db.PendingVerifies.Add(new PendingVerify { Email = emailLower, Username = req.Username, PublicKey = req.PublicKey, Code = code, ExpiresAt = DateTime.UtcNow.AddMinutes(15) });
            await _db.SaveChangesAsync();
            try { await _email.SendVerificationAsync(req.Email, req.Username, code); }
            catch { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine($"\n  [DEBUG] Code for {req.Email}: {code}\n"); Console.ResetColor(); }
            return Ok();
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyReq req)
        {
            var emailLower = req.Email.ToLowerInvariant();
            var pending = await _db.PendingVerifies.FirstOrDefaultAsync(p => p.Email == emailLower && p.Code == req.Code && p.ExpiresAt > DateTime.UtcNow);
            if (pending == null) return BadRequest("Invalid or expired code.");
            var user = new User { Email = pending.Email, Username = pending.Username, PublicKey = pending.PublicKey, IsVerified = true };
            _db.Users.Add(user);
            _db.PendingVerifies.Remove(pending);
            await _db.SaveChangesAsync();
            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"  [+] Registered: {user.Username}"); Console.ResetColor();
            return Ok(new { token = _tokens.Generate(user), username = user.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginReq req)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant() && u.IsVerified);
            if (user == null) return Unauthorized("User not found.");
            Console.WriteLine($"  [→] Login: {user.Username}");
            return Ok(new { token = _tokens.Generate(user), username = user.Username });
        }

        [HttpPost("request-delete")]
        [Authorize]
        public async Task<IActionResult> RequestDelete()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();
            var code = RandomNumberGenerator.GetInt32(100_000, 999_999).ToString();
            _db.PendingVerifies.RemoveRange(_db.PendingVerifies.Where(p => p.Email == user.Email));
            _db.PendingVerifies.Add(new PendingVerify { Email = user.Email, Username = user.Username, PublicKey = "", Code = code, ExpiresAt = DateTime.UtcNow.AddMinutes(15) });
            await _db.SaveChangesAsync();
            try { await _email.SendVerificationAsync(user.Email, user.Username, code); }
            catch { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine($"\n  [DEBUG] Delete code for {user.Email}: {code}\n"); Console.ResetColor(); }
            return Ok();
        }

        [HttpPost("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteReq req)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();
            var pending = await _db.PendingVerifies.FirstOrDefaultAsync(p => p.Email == user.Email && p.Code == req.Code && p.ExpiresAt > DateTime.UtcNow);
            if (pending == null) return BadRequest("Invalid or expired code.");
            var convos = _db.Conversations.Where(c => c.User1Id == userId || c.User2Id == userId);
            foreach (var c in convos)
                _db.Messages.RemoveRange(_db.Messages.Where(m => m.ConversationId == c.Id));
            _db.Conversations.RemoveRange(convos);
            _db.PendingVerifies.RemoveRange(_db.PendingVerifies.Where(p => p.Email == user.Email));
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"  [-] Deleted: {user.Username}"); Console.ResetColor();
            return Ok();
        }
    }

    public record RegisterReq(string Username, string Email, string PublicKey);
    public record VerifyReq(string Email, string Code);
    public record LoginReq(string Email, string Password);
    public record DeleteReq(string Code, string Password);
}
