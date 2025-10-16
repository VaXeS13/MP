using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.Booths;
using MP.Permissions;
using Volo.Abp.Application.Dtos;

namespace MP.Controllers
{
    [ApiController]
    [Route("api/app/booth")]
    [Authorize(MPPermissions.Booths.Default)]
    public class BoothController : ControllerBase
    {
        private readonly IBoothAppService _boothAppService;

        public BoothController(IBoothAppService boothAppService)
        {
            _boothAppService = boothAppService;
        }

        [HttpPut("{id}")]
        [Authorize(MPPermissions.Booths.Edit)]
        public async Task<BoothDto> Update(Guid id, UpdateBoothDto input)
        {
            return await _boothAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        [Authorize(MPPermissions.Booths.Delete)]
        public async Task Delete(Guid id)
        {
            await _boothAppService.DeleteAsync(id);
        }
    }
}
