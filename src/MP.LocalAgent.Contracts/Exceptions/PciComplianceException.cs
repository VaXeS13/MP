namespace MP.LocalAgent.Contracts.Exceptions
{
    /// <summary>
    /// Exception thrown when PCI DSS compliance rules are violated
    /// </summary>
    public class PciComplianceException : Exception
    {
        public PciComplianceException(string message)
            : base(message)
        {
        }

        public PciComplianceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
