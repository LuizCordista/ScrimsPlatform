namespace TeamService.Client;

public class UserServiceClient(HttpClient httpClient) : IUserServiceClient
{
    public async Task<bool> UserExistsAsync(Guid userId)
    {
        var response = await httpClient.GetAsync($"api/user/{userId}");
        return response.IsSuccessStatusCode;
    }
}
