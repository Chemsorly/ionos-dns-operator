namespace IonosDns;

/// <summary>
/// Exception thrown when an unexpected error occurs during IONOS DNS API operations.
/// </summary>
public class IonosDnsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the IonosDnsException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public IonosDnsException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the IonosDnsException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public IonosDnsException(string message, Exception innerException) : base(message, innerException) { }
}
