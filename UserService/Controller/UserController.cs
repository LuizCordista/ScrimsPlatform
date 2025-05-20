using Microsoft.AspNetCore.Mvc;
using UserService.Dto;
using UserService.Model;
using UserService.Service;

namespace UserService.Controller;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequestDto userRegisterRequestDto)
    {
        var user = new User(userRegisterRequestDto.Username, userRegisterRequestDto.Email,
            userRegisterRequestDto.Password);

        var createdUser = await userService.CreateUserAsync(user);

        return CreatedAtAction(nameof(Register), new { id = createdUser.Id }, new UserRegisterResponseDto(
                createdUser.Id,
                createdUser.Username,
                createdUser.Email,
                createdUser.CreatedAt
            )
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequestDto)
    {
        var loginResponse = await userService.LoginAsync(loginRequestDto.Email, loginRequestDto.Password);

        return Ok(loginResponse);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);

        return Ok(new UserResponseDto(user.Id, user.Username, user.Email, user.CreatedAt, user.UpdatedAt));
    }
}