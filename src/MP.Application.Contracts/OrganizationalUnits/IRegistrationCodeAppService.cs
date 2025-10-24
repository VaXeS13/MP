using MP.OrganizationalUnits.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.OrganizationalUnits
{
    public interface IRegistrationCodeAppService : IApplicationService
    {
        /// <summary>
        /// Generates a new registration code for an organizational unit
        /// </summary>
        Task<RegistrationCodeDto> GenerateCodeAsync(Guid organizationalUnitId, CreateRegistrationCodeDto input);

        /// <summary>
        /// Joins user to organizational unit using a registration code
        /// </summary>
        Task<JoinUnitResultDto> JoinUnitWithCodeAsync(string code);

        /// <summary>
        /// Validates if a registration code is valid
        /// </summary>
        Task<ValidateCodeResultDto> ValidateCodeAsync(string code);

        /// <summary>
        /// Deactivates a registration code
        /// </summary>
        Task DeactivateCodeAsync(Guid codeId);

        /// <summary>
        /// Lists all registration codes for an organizational unit
        /// </summary>
        Task<List<RegistrationCodeDto>> ListCodesAsync(Guid organizationalUnitId);
    }
}
