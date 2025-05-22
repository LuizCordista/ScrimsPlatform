namespace TeamService.CustomException;

public class TeamAlreadyExistsException : Exception
{
    public TeamAlreadyExistsException(string message) : base(message)
    {
    }
}
