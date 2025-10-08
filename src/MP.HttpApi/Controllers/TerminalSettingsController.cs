using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using MP.Application.Contracts.Terminals;

namespace MP.HttpApi.Controllers
{
    [Route("api/app/terminal-settings")]
    public class TerminalSettingsController : AbpControllerBase
    {
        private readonly ITerminalSettingsAppService _terminalSettingsAppService;

        public TerminalSettingsController(ITerminalSettingsAppService terminalSettingsAppService)
        {
            _terminalSettingsAppService = terminalSettingsAppService;
        }

        [HttpGet("current")]
        public virtual Task<TerminalSettingsDto?> GetCurrentTenantSettingsAsync()
        {
            return _terminalSettingsAppService.GetCurrentTenantSettingsAsync();
        }

        [HttpPost]
        public virtual Task<TerminalSettingsDto> CreateAsync(CreateTerminalSettingsDto input)
        {
            return _terminalSettingsAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        public virtual Task<TerminalSettingsDto> UpdateAsync(Guid id, UpdateTerminalSettingsDto input)
        {
            return _terminalSettingsAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        public virtual Task DeleteAsync(Guid id)
        {
            return _terminalSettingsAppService.DeleteAsync(id);
        }

        [HttpGet("providers")]
        public virtual Task<List<TerminalProviderInfoDto>> GetAvailableProvidersAsync()
        {
            return _terminalSettingsAppService.GetAvailableProvidersAsync();
        }
    }
}