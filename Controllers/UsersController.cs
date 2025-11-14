using Microsoft.AspNetCore.Mvc;
using UsersService.Data;
using UsersService.Models;
using Microsoft.EntityFrameworkCore;
using UsersService.Dtos;
using UsersService.ApplicationServices;
using UsersService.Dtos.ResponseDtos;
using AutoMapper;

namespace UsersService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly AuthService _authService;
        private readonly IMapper _mapper;
        public UsersController(AppDBContext context, AuthService authService, IMapper mapper)
        {
            _context = context;
            _authService = authService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserInfoResponseDto>>> GetUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return _mapper.Map<List<User>, List<UserInfoResponseDto>>(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserInfoResponseDto>> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return _mapper.Map<User, UserInfoResponseDto>(user);
        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterUserAsync(RegisterDto dto)
        {
            await _authService.RegisterUserAsync(dto);
            return Created("", new { message = "Account Created Successfully" });
        }
    
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid credentials");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserAsync(Guid id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }
            _context.Entry(user).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok();
        }

    }
}

