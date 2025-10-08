using System;

namespace MP.Domain.Terminals
{
    /// <summary>
    /// Terminal payment exception
    /// </summary>
    public class TerminalException : Exception
    {
        public string? ErrorCode { get; set; }

        public TerminalException(string message) : base(message) { }

        public TerminalException(string message, Exception innerException)
            : base(message, innerException) { }

        public TerminalException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public TerminalException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
