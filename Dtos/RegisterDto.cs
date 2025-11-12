using System.ComponentModel.DataAnnotations;

namespace UsersService.Dtos;
public class RegisterDto
{
    public required string Name { get; set; }
    public required string Email { get; set; }

    [MinLength(4, ErrorMessage = "Password must be at least 6 characters.")]
    public required string Password { get; set; }
}
