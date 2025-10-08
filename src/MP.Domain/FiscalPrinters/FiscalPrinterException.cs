using System;

namespace MP.Domain.FiscalPrinters
{
    /// <summary>
    /// Fiscal printer exception
    /// </summary>
    public class FiscalPrinterException : Exception
    {
        public string? ErrorCode { get; set; }

        public FiscalPrinterException(string message) : base(message) { }

        public FiscalPrinterException(string message, Exception innerException)
            : base(message, innerException) { }

        public FiscalPrinterException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public FiscalPrinterException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
