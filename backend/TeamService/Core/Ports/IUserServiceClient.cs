namespace TeamService.Core.Ports;

public interface IUserServiceClient
{
    Task<bool> UserExistsAsync(Guid userId);
}
