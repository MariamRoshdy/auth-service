namespace UsersService.Dtos.ResponseDtos;
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiry { get; set; }
    public string TokenType { get; set; } = "Bearer";
}