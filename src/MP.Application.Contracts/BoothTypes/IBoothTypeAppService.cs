using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.BoothTypes
{
    public interface IBoothTypeAppService : ICrudAppService<
        BoothTypeDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateBoothTypeDto,
        UpdateBoothTypeDto>
    {
        Task<List<BoothTypeDto>> GetActiveTypesAsync();
        Task<BoothTypeDto> ActivateAsync(Guid id);
        Task<BoothTypeDto> DeactivateAsync(Guid id);
    }
}