﻿namespace UserService.Core.Exceptions;

public class InvalidPasswordException : Exception
{
    public InvalidPasswordException(string message) : base(message)
    {
    }
}