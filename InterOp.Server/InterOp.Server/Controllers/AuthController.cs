using BCrypt.Net;
using InterOp.Server.Auth;
using InterOp.Server.Data;
using InterOp.Server.Domain;
using InterOp.Server.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterOp.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Username i password su obavezni." });

        var exists = await _db.Users.AnyAsync(u => u.Username == req.Username);
        if (exists) return Conflict(new { message = "Korisnik već postoji." });

        var user = new AppUser
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created($"/api/users/{user.Id}", new { user.Id, user.Username });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _db.Users.Include(u => u.RefreshTokens)
                                  .FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Neispravno korisničko ime ili lozinka." });

        var (access, accessExp) = _tokens.CreateAccessToken(user);
        var (refresh, refreshExp) = _tokens.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refresh,
            ExpiresUtc = refreshExp
        });
        await _db.SaveChangesAsync();

        return new AuthResponse(access, accessExp, refresh, refreshExp);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest req)
    {
        var principal = _tokens.GetPrincipalFromExpiredToken(req.AccessToken);
        if (principal is null) return Unauthorized(new { message = "Neispravan access token." });

        var userId = int.Parse(principal.Claims.First(c => c.Type == "sub").Value);
        var user = await _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized();

        var tokenEntity = user.RefreshTokens.FirstOrDefault(t => t.Token == req.RefreshToken);
        if (tokenEntity is null || !tokenEntity.IsActive) return Unauthorized(new { message = "Nevažeći refresh token." });

 
        tokenEntity.RevokedUtc = DateTime.UtcNow;

        var (newRefresh, newRefreshExp) = _tokens.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefresh,
            ExpiresUtc = newRefreshExp
        });

        var (access, accessExp) = _tokens.CreateAccessToken(user);
        await _db.SaveChangesAsync();

        return new AuthResponse(access, accessExp, newRefresh, newRefreshExp);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] string refreshToken)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (rt is null || !rt.IsActive) return NotFound();
        rt.RevokedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
