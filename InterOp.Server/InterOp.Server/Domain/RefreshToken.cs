namespace InterOp.Server.Domain;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public string Token { get; set; } = "";
    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedUtc { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsActive => RevokedUtc is null && DateTime.UtcNow < ExpiresUtc;
}
