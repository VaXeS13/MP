using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MP.Booths;
using MP.Domain.Booths;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace MP.Controllers
{
    [RemoteService(Name = "Default")]
    [Area("app")]
    [ControllerName("Booth")]
    [Route("api/app/booths")]
    public class BoothController : AbpControllerBase, IBoothAppService
    {
        private readonly IBoothAppService _boothAppService;

        public BoothController(IBoothAppService boothAppService)
        {
            _boothAppService = boothAppService;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<BoothDto> GetAsync(Guid id)
        {
            return await _boothAppService.GetAsync(id);
        }

        [HttpGet]
        public async Task<PagedResultDto<BoothListDto>> GetListAsync(GetBoothListDto input)
        {
            return await _boothAppService.GetListAsync(input);
        }

        [HttpPost]
        public async Task<BoothDto> CreateAsync(CreateBoothDto input)
        {
            return await _boothAppService.CreateAsync(input);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<BoothDto> UpdateAsync(Guid id, UpdateBoothDto input)
        {
            return await _boothAppService.UpdateAsync(id, input);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task DeleteAsync(Guid id)
        {
            await _boothAppService.DeleteAsync(id);
        }

        [HttpGet]
        [Route("available")]
        public async Task<List<BoothDto>> GetAvailableBoothsAsync()
        {
            return await _boothAppService.GetAvailableBoothsAsync();
        }

        [HttpPut]
        [Route("{id}/change-status")]
        public async Task<BoothDto> ChangeStatusAsync(Guid id, [FromQuery] BoothStatus newStatus)
        {
            return await _boothAppService.ChangeStatusAsync(id, newStatus);
        }

        [HttpGet]
        [Route("my-booths")]
        public async Task<PagedResultDto<BoothListDto>> GetMyBoothsAsync(GetBoothListDto input)
        {
            return await _boothAppService.GetMyBoothsAsync(input);
        }

        [HttpPost]
        [Route("manual-reservation")]
        public async Task<BoothDto> CreateManualReservationAsync(CreateManualReservationDto input)
        {
            return await _boothAppService.CreateManualReservationAsync(input);
        }
    }
}