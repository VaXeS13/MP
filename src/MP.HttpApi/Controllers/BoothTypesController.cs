using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MP.Application.Contracts.BoothTypes;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace MP.Controllers
{
    [RemoteService(Name = "Default")]
    [Area("app")]
    [Route("api/app/booth-types")]
    public class BoothTypesController : AbpControllerBase, IBoothTypeAppService
    {
        private readonly IBoothTypeAppService _boothTypeAppService;

        public BoothTypesController(IBoothTypeAppService boothTypeAppService)
        {
            _boothTypeAppService = boothTypeAppService;
        }

        [HttpGet]
        public virtual Task<PagedResultDto<BoothTypeDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _boothTypeAppService.GetListAsync(input);
        }

        [HttpGet]
        [Route("{id}")]
        public virtual Task<BoothTypeDto> GetAsync(Guid id)
        {
            return _boothTypeAppService.GetAsync(id);
        }

        [HttpPost]
        public virtual Task<BoothTypeDto> CreateAsync(CreateBoothTypeDto input)
        {
            return _boothTypeAppService.CreateAsync(input);
        }

        [HttpPut]
        [Route("{id}")]
        public virtual Task<BoothTypeDto> UpdateAsync(Guid id, UpdateBoothTypeDto input)
        {
            return _boothTypeAppService.UpdateAsync(id, input);
        }

        [HttpDelete]
        [Route("{id}")]
        public virtual Task DeleteAsync(Guid id)
        {
            return _boothTypeAppService.DeleteAsync(id);
        }

        [HttpGet]
        [Route("active")]
        public virtual Task<List<BoothTypeDto>> GetActiveTypesAsync()
        {
            return _boothTypeAppService.GetActiveTypesAsync();
        }

        [HttpPost]
        [Route("{id}/activate")]
        public virtual Task<BoothTypeDto> ActivateAsync(Guid id)
        {
            return _boothTypeAppService.ActivateAsync(id);
        }

        [HttpPost]
        [Route("{id}/deactivate")]
        public virtual Task<BoothTypeDto> DeactivateAsync(Guid id)
        {
            return _boothTypeAppService.DeactivateAsync(id);
        }
    }
}