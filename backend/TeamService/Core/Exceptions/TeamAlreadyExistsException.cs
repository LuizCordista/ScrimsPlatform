namespace TeamService.Core.Exceptions;

public class TeamAlreadyExistsException : Exception
{
    public TeamAlreadyExistsException(string message) : base(message)
    {
    }
}
