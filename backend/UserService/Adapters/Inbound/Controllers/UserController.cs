using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await userService.GetAllUsersAsync();
        var userDtos = users.Select(user => new UserResponseDto(user.Id, user.Username, user.Email, user.CreatedAt, user.UpdatedAt)).ToList();
        return Ok(userDtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);

        return Ok(new UserResponseDto(user.Id, user.Username, user.Email, user.CreatedAt, user.UpdatedAt));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsersByUsername([FromQuery] string username)
    {
        var users = await userService.SearchUserByUsernameAsync(username);

        var userDtos = users.Select(user => new UserResponseDto(user.Id, user.Username, user.Email,
            user.CreatedAt, user.UpdatedAt)).ToList();

        return Ok(userDtos);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetAuthenticatedUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await userService.GetUserByIdAsync(Guid.Parse(userId));

        return Ok(new UserResponseDto(user.Id, user.Username, user.Email, user.CreatedAt, user.UpdatedAt));
    }

    [HttpPut("me/password")]
    [Authorize]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequestDto updatePasswordRequestDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        await userService.UpdateUserPasswordAsync(Guid.Parse(userId),
            updatePasswordRequestDto.CurrentPassword, updatePasswordRequestDto.NewPassword);

        return Ok(new UpdatePasswordResponseDto(true, "Password updated successfully."));
    }
}