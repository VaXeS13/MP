using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.Terminals
{
    public interface ITerminalSettingsAppService : IApplicationService
    {
        Task<TerminalSettingsDto?> GetCurrentTenantSettingsAsync();
        Task<TerminalSettingsDto> CreateAsync(CreateTerminalSettingsDto input);
        Task<TerminalSettingsDto> UpdateAsync(Guid id, UpdateTerminalSettingsDto input);
        Task DeleteAsync(Guid id);
        Task<List<TerminalProviderInfoDto>> GetAvailableProvidersAsync();
    }
}