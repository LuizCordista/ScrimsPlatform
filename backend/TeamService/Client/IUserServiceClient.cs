namespace TeamService.Client;

public interface IUserServiceClient
{
    Task<bool> UserExistsAsync(Guid userId);
}
