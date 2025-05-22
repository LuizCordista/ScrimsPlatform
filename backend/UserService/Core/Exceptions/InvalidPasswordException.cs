namespace UserService.CustomException;

public class InvalidPasswordException : Exception
{
    public InvalidPasswordException(string message) : base(message)
    {
    }
}