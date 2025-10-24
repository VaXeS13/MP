using System;
using Volo.Abp;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Exception thrown when trying to access the current organizational unit
    /// when no organizational unit is set in the current context
    /// </summary>
    public class CurrentOrganizationalUnitNotSetException : BusinessException
    {
        public CurrentOrganizationalUnitNotSetException()
            : base(
                code: "CURRENT_ORGANIZATIONAL_UNIT_NOT_SET",
                message: "No organizational unit is currently set in the context")
        {
        }

        public CurrentOrganizationalUnitNotSetException(string message)
            : base(
                code: "CURRENT_ORGANIZATIONAL_UNIT_NOT_SET",
                message: message)
        {
        }

        public CurrentOrganizationalUnitNotSetException(string message, Exception innerException)
            : base(
                code: "CURRENT_ORGANIZATIONAL_UNIT_NOT_SET",
                message: message,
                innerException: innerException)
        {
        }
    }
}
