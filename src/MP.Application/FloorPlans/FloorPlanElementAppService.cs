using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MP.Domain.FloorPlans;
using MP.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.FloorPlans
{
    [Authorize(MPPermissions.FloorPlans.Default)]
    public class FloorPlanElementAppService : ApplicationService, IFloorPlanElementAppService
    {
        private readonly IFloorPlanElementRepository _floorPlanElementRepository;
        private readonly IFloorPlanRepository _floorPlanRepository;

        public FloorPlanElementAppService(
            IFloorPlanElementRepository floorPlanElementRepository,
            IFloorPlanRepository floorPlanRepository)
        {
            _floorPlanElementRepository = floorPlanElementRepository;
            _floorPlanRepository = floorPlanRepository;
        }

        public async Task<FloorPlanElementDto> GetAsync(Guid id)
        {
            var element = await _floorPlanElementRepository.GetAsync(id);
            return ObjectMapper.Map<FloorPlanElement, FloorPlanElementDto>(element);
        }

        public async Task<PagedResultDto<FloorPlanElementDto>> GetListAsync(GetFloorPlanElementListDto input)
        {
            var queryable = await _floorPlanElementRepository.GetQueryableAsync();

            if (input.FloorPlanId.HasValue)
            {
                queryable = queryable.Where(x => x.FloorPlanId == input.FloorPlanId.Value);
            }

            if (input.ElementType.HasValue)
            {
                queryable = queryable.Where(x => x.ElementType == input.ElementType.Value);
            }

            var totalCount = queryable.Count();

            var items = queryable
                .OrderBy(x => x.ElementType)
                .ThenBy(x => x.X)
                .ThenBy(x => x.Y)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            var dtos = ObjectMapper.Map<List<FloorPlanElement>, List<FloorPlanElementDto>>(items);

            return new PagedResultDto<FloorPlanElementDto>(totalCount, dtos);
        }

        public async Task<List<FloorPlanElementDto>> GetListByFloorPlanAsync(Guid floorPlanId)
        {
            var elements = await _floorPlanElementRepository.GetListByFloorPlanAsync(floorPlanId);
            return ObjectMapper.Map<List<FloorPlanElement>, List<FloorPlanElementDto>>(elements);
        }

        [Authorize(MPPermissions.FloorPlans.Edit)]
        public async Task<FloorPlanElementDto> CreateAsync(Guid floorPlanId, CreateFloorPlanElementDto input)
        {
            var floorPlan = await _floorPlanRepository.GetAsync(floorPlanId);

            if (floorPlan == null)
            {
                throw new BusinessException("FLOOR_PLAN_NOT_FOUND")
                    .WithData("FloorPlanId", floorPlanId);
            }

            var element = new FloorPlanElement(
                GuidGenerator.Create(),
                floorPlanId,
                input.ElementType,
                input.X,
                input.Y,
                input.Width,
                input.Height,
                input.Rotation,
                input.Color,
                input.Text,
                input.IconName,
                input.Thickness,
                input.Opacity,
                input.Direction
            );

            await _floorPlanElementRepository.InsertAsync(element);
            await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<FloorPlanElement, FloorPlanElementDto>(element);
        }

        [Authorize(MPPermissions.FloorPlans.Edit)]
        public async Task<FloorPlanElementDto> UpdateAsync(Guid id, UpdateFloorPlanElementDto input)
        {
            var element = await _floorPlanElementRepository.GetAsync(id);

            element.UpdateProperties(
                input.X,
                input.Y,
                input.Width,
                input.Height,
                input.Rotation,
                input.Color,
                input.Text,
                input.IconName,
                input.Thickness,
                input.Opacity,
                input.Direction
            );

            await _floorPlanElementRepository.UpdateAsync(element);
            await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<FloorPlanElement, FloorPlanElementDto>(element);
        }

        [Authorize(MPPermissions.FloorPlans.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _floorPlanElementRepository.DeleteAsync(id);
        }

        [Authorize(MPPermissions.FloorPlans.Delete)]
        public async Task DeleteByFloorPlanAsync(Guid floorPlanId)
        {
            await _floorPlanElementRepository.DeleteByFloorPlanAsync(floorPlanId);
        }
    }
}
