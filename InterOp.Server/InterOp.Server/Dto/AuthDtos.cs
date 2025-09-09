namespace InterOp.Server.Dto;

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc);

public record RefreshRequest(string AccessToken, string RefreshToken);
