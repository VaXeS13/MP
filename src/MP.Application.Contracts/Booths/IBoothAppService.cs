using MP.Domain.Booths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Booths
{
    public interface IBoothAppService : IApplicationService
    {
        Task<BoothDto> GetAsync(Guid id);

        Task<PagedResultDto<BoothListDto>> GetListAsync(GetBoothListDto input);

        Task<BoothDto> CreateAsync(CreateBoothDto input);

        Task<BoothDto> UpdateAsync(Guid id, UpdateBoothDto input);

        Task DeleteAsync(Guid id);

        Task<List<BoothDto>> GetAvailableBoothsAsync();

        Task<BoothDto> ChangeStatusAsync(Guid id, BoothStatus newStatus);

        Task<PagedResultDto<BoothListDto>> GetMyBoothsAsync(GetBoothListDto input);

        Task<BoothDto> CreateManualReservationAsync(CreateManualReservationDto input);
    }
}
