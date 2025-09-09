using System.ComponentModel.DataAnnotations;

namespace InterOp.Server.Domain;

public class AppUser
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Username { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public List<RefreshToken> RefreshTokens { get; set; } = new();
}
