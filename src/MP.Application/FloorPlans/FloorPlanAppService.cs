using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MP.Domain.FloorPlans;
using MP.Domain.Rentals;
using MP.Permissions;
using MP.Rentals;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using MP.Domain.OrganizationalUnits;

namespace MP.FloorPlans
{
    [Authorize(MPPermissions.FloorPlans.Default)]
    public class FloorPlanAppService : ApplicationService, IFloorPlanAppService
    {
        private readonly IFloorPlanRepository _floorPlanRepository;
        private readonly IFloorPlanBoothRepository _floorPlanBoothRepository;
        private readonly IFloorPlanElementRepository _floorPlanElementRepository;
        private readonly IRepository<Domain.Booths.Booth, Guid> _boothRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IDistributedCache<FloorPlanDto> _floorPlanCache;
        private readonly IDistributedCache<List<FloorPlanDto>> _floorPlanListCache;
        private readonly ICurrentOrganizationalUnit _currentOrganizationalUnit;

        public FloorPlanAppService(
            IFloorPlanRepository floorPlanRepository,
            IFloorPlanBoothRepository floorPlanBoothRepository,
            IFloorPlanElementRepository floorPlanElementRepository,
            IRepository<Domain.Booths.Booth, Guid> boothRepository,
            IRepository<Rental, Guid> rentalRepository,
            IDistributedCache<FloorPlanDto> floorPlanCache,
            IDistributedCache<List<FloorPlanDto>> floorPlanListCache,
            ICurrentOrganizationalUnit currentOrganizationalUnit)
        {
            _floorPlanRepository = floorPlanRepository;
            _floorPlanBoothRepository = floorPlanBoothRepository;
            _floorPlanElementRepository = floorPlanElementRepository;
            _boothRepository = boothRepository;
            _rentalRepository = rentalRepository;
            _floorPlanCache = floorPlanCache;
            _floorPlanListCache = floorPlanListCache;
            _currentOrganizationalUnit = currentOrganizationalUnit;
        }

        public async Task<FloorPlanDto> GetAsync(Guid id)
        {
            var cacheKey = $"FloorPlan_{id}";

            var cachedData = await _floorPlanCache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var floorPlan = await _floorPlanRepository.GetAsync(id);
                    var booths = await _floorPlanBoothRepository.GetListByFloorPlanAsync(id);
                    var elements = await _floorPlanElementRepository.GetListByFloorPlanAsync(id);

                    var dto = ObjectMapper.Map<FloorPlan, FloorPlanDto>(floorPlan);
                    dto.Booths = ObjectMapper.Map<List<FloorPlanBooth>, List<FloorPlanBoothDto>>(booths);
                    dto.Elements = ObjectMapper.Map<List<FloorPlanElement>, List<FloorPlanElementDto>>(elements);

                    return dto;
                },
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                }
            );

            return cachedData;
        }

        public async Task<PagedResultDto<FloorPlanDto>> GetListAsync(GetFloorPlanListDto input)
        {
            var tenantId = input.TenantId ?? CurrentTenant.Id;

            List<FloorPlan> sourceFloorPlans;

            // Handle case where tenantId is null (development scenario)
            if (tenantId == null)
            {
                // In development, get all floor plans regardless of tenant
                var allFloorPlans = await _floorPlanRepository.GetListAsync();
                sourceFloorPlans = input.IsActive.HasValue
                    ? allFloorPlans.Where(x => x.IsActive == input.IsActive.Value).ToList()
                    : allFloorPlans;
            }
            else
            {
                sourceFloorPlans = await _floorPlanRepository.GetListByTenantAsync(tenantId, input.IsActive);
            }

            var filteredPlans = sourceFloorPlans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                filteredPlans = filteredPlans.Where(x => x.Name.Contains(input.Filter));
            }

            if (input.Level.HasValue)
            {
                filteredPlans = filteredPlans.Where(x => x.Level == input.Level.Value);
            }

            var totalCount = filteredPlans.Count();
            var items = filteredPlans
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            var dtos = new List<FloorPlanDto>();

            foreach (var floorPlan in items)
            {
                var dto = ObjectMapper.Map<FloorPlan, FloorPlanDto>(floorPlan);
                var booths = await _floorPlanBoothRepository.GetListByFloorPlanAsync(floorPlan.Id);
                var elements = await _floorPlanElementRepository.GetListByFloorPlanAsync(floorPlan.Id);
                dto.Booths = ObjectMapper.Map<List<FloorPlanBooth>, List<FloorPlanBoothDto>>(booths);
                dto.Elements = ObjectMapper.Map<List<FloorPlanElement>, List<FloorPlanElementDto>>(elements);
                dtos.Add(dto);
            }

            return new PagedResultDto<FloorPlanDto>(totalCount, dtos);
        }

        private async Task<List<FloorPlanDto>> LoadFloorPlansAsync(Guid? tenantId, bool? isActive)
        {
            // Handle case where tenantId is null (development scenario)
            if (tenantId == null)
            {
                var allFloorPlans = await _floorPlanRepository.GetListAsync();
                var filteredPlans = isActive.HasValue
                    ? allFloorPlans.Where(x => x.IsActive == isActive.Value).ToList()
                    : allFloorPlans;

                var resultDtos = new List<FloorPlanDto>();
                foreach (var floorPlan in filteredPlans)
                {
                    var dto = ObjectMapper.Map<FloorPlan, FloorPlanDto>(floorPlan);
                    var booths = await _floorPlanBoothRepository.GetListByFloorPlanAsync(floorPlan.Id);
                    var elements = await _floorPlanElementRepository.GetListByFloorPlanAsync(floorPlan.Id);
                    dto.Booths = ObjectMapper.Map<List<FloorPlanBooth>, List<FloorPlanBoothDto>>(booths);
                    dto.Elements = ObjectMapper.Map<List<FloorPlanElement>, List<FloorPlanElementDto>>(elements);
                    resultDtos.Add(dto);
                }
                return resultDtos;
            }

            var floorPlans = await _floorPlanRepository.GetListByTenantAsync(tenantId, isActive);

            var dtos = new List<FloorPlanDto>();

            foreach (var floorPlan in floorPlans)
            {
                var dto = ObjectMapper.Map<FloorPlan, FloorPlanDto>(floorPlan);
                var booths = await _floorPlanBoothRepository.GetListByFloorPlanAsync(floorPlan.Id);
                var elements = await _floorPlanElementRepository.GetListByFloorPlanAsync(floorPlan.Id);
                dto.Booths = ObjectMapper.Map<List<FloorPlanBooth>, List<FloorPlanBoothDto>>(booths);
                dto.Elements = ObjectMapper.Map<List<FloorPlanElement>, List<FloorPlanElementDto>>(elements);
                dtos.Add(dto);
            }

            return dtos;
        }

        [Authorize(MPPermissions.FloorPlans.Create)]
        public async Task<FloorPlanDto> CreateAsync(CreateFloorPlanDto input)
        {
            var tenantId = CurrentTenant.Id;
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED")
                .WithData("message", "Current organizational unit context is not set");

            // Allow same name for different levels - the combination of name + level should be unique
            // Database will enforce uniqueness if needed

            var floorPlan = new FloorPlan(
                GuidGenerator.Create(),
                input.Name,
                input.Level,
                input.Width,
                input.Height,
                organizationalUnitId,
                tenantId);

            await _floorPlanRepository.InsertAsync(floorPlan);

            // Add booths if provided
            var floorPlanBooths = new List<FloorPlanBooth>();
            if (input.Booths.Any())
            {
                foreach (var boothDto in input.Booths)
                {
                    // Verify booth exists
                    await _boothRepository.GetAsync(boothDto.BoothId);

                    var floorPlanBooth = new FloorPlanBooth(
                        GuidGenerator.Create(),
                        floorPlan.Id,
                        boothDto.BoothId,
                        boothDto.X,
                        boothDto.Y,
                        boothDto.Width,
                        boothDto.Height,
                        boothDto.Rotation);

                    await _floorPlanBoothRepository.InsertAsync(floorPlanBooth);
                    floorPlanBooths.Add(floorPlanBooth);
                }
            }

            // Add elements if provided
            var floorPlanElements = new List<FloorPlanElement>();
            if (input.Elements.Any())
            {
                foreach (var elementDto in input.Elements)
                {
                    var floorPlanElement = new FloorPlanElement(
                        GuidGenerator.Create(),
                        floorPlan.Id,
                        elementDto.ElementType,
                        elementDto.X,
                        elementDto.Y,
                        elementDto.Width,
                        elementDto.Height,
                        elementDto.Rotation,
                        elementDto.Color,
                        elementDto.Text,
                        elementDto.IconName,
                        elementDto.Thickness,
                        elementDto.Opacity,
                        elementDto.Direction);

                    await _floorPlanElementRepository.InsertAsync(floorPlanElement);
                    floorPlanElements.Add(floorPlanElement);
                }
            }

            // Save changes to ensure the floor plan is persisted
            await CurrentUnitOfWork.SaveChangesAsync();

            // Invalidate cache
            await InvalidateCacheAsync(floorPlan.Id, tenantId);

            // Return the created floor plan DTO
            var dto = ObjectMapper.Map<FloorPlan, FloorPlanDto>(floorPlan);
            dto.Booths = ObjectMapper.Map<List<FloorPlanBooth>, List<FloorPlanBoothDto>>(floorPlanBooths);
            dto.Elements = ObjectMapper.Map<List<FloorPlanElement>, List<FloorPlanElementDto>>(floorPlanElements);

            return dto;
        }

        [Authorize(MPPermissions.FloorPlans.Edit)]
        public async Task<FloorPlanDto> UpdateAsync(Guid id, UpdateFloorPlanDto input)
        {
            var floorPlan = await _floorPlanRepository.GetAsync(id);

            // Allow same name for different levels
            floorPlan.SetName(input.Name);
            floorPlan.SetLevel(input.Level);
            floorPlan.SetDimensions(input.Width, input.Height);

            await _floorPlanRepository.UpdateAsync(floorPlan);

            // Update booths - ExecuteDeleteAsync commits immediately
            await _floorPlanBoothRepository.DeleteByFloorPlanAsync(id);

            var floorPlanBooths = new List<FloorPlanBooth>();
            if (input.Booths.Any())
            {
                foreach (var boothDto in input.Booths)
                {
                    // Verify booth exists
                    await _boothRepository.GetAsync(boothDto.BoothId);

                    var floorPlanBooth = new FloorPlanBooth(
                        GuidGenerator.Create(),
                        floorPlan.Id,
                        boothDto.BoothId,
                        boothDto.X,
                        boothDto.Y,
                        boothDto.Width,
                        boothDto.Height,
                        boothDto.Rotation);

                    await _floorPlanBoothRepository.InsertAsync(floorPlanBooth);
                    floorPlanBooths.Add(floorPlanBooth);
                }
            }

            // Update elements - ExecuteDeleteAsync commits immediately
            await _floorPlanElementRepository.DeleteByFloorPlanAsync(id);

            var floorPlanElements = new List<FloorPlanElement>();
            if (input.Elements.Any())
            {
                foreach (var elementDto in input.Elements)
                {
                    var floorPlanElement = new FloorPlanElement(
                        GuidGenerator.Create(),
                        floorPlan.Id,
                        elementDto.ElementType,
                        elementDto.X,
                        elementDto.Y,
                        elementDto.Width,
                        elementDto.Height,
                        elementDto.Rotation,
                        elementDto.Color,
                        elementDto.Text,
                        elementDto.IconName,
                        elementDto.Thickness,
                        elementDto.Opacity,
                        elementDto.Direction);

                    await _floorPlanElementRepository.InsertAsync(floorPlanElement);
                    floorPlanElements.Add(floorPlanElement);
                }
            }

            // Invalidate cache
            await InvalidateCacheAsync(id, floorPlan.TenantId);

            // Return the updated floor plan DTO
            var dto = ObjectMapper.Map<FloorPlan, FloorPlanDto>(floorPlan);
            dto.Booths = ObjectMapper.Map<List<FloorPlanBooth>, List<FloorPlanBoothDto>>(floorPlanBooths);
            dto.Elements = ObjectMapper.Map<List<FloorPlanElement>, List<FloorPlanElementDto>>(floorPlanElements);

            return dto;
        }

        [Authorize(MPPermissions.FloorPlans.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            var floorPlan = await _floorPlanRepository.GetAsync(id);
            var tenantId = floorPlan.TenantId;

            await _floorPlanBoothRepository.DeleteByFloorPlanAsync(id);
            await _floorPlanElementRepository.DeleteByFloorPlanAsync(id);
            await _floorPlanRepository.DeleteAsync(id);

            await InvalidateCacheAsync(id, tenantId);
        }

        [Authorize(MPPermissions.FloorPlans.Publish)]
        public async Task<FloorPlanDto> PublishAsync(Guid id)
        {
            var floorPlan = await _floorPlanRepository.GetAsync(id);
            floorPlan.Publish();
            await _floorPlanRepository.UpdateAsync(floorPlan);

            await InvalidateCacheAsync(id, floorPlan.TenantId);

            return await GetAsync(id);
        }

        [Authorize(MPPermissions.FloorPlans.Edit)]
        public async Task<FloorPlanDto> DeactivateAsync(Guid id)
        {
            var floorPlan = await _floorPlanRepository.GetAsync(id);
            floorPlan.Deactivate();
            await _floorPlanRepository.UpdateAsync(floorPlan);

            await InvalidateCacheAsync(id, floorPlan.TenantId);

            return await GetAsync(id);
        }

        [HttpGet("/api/app/floor-plan/{floorPlanId}/booths")]
        public async Task<List<FloorPlanBoothDto>> GetBoothsAsync(Guid floorPlanId)
        {
            var booths = await _floorPlanBoothRepository.GetListByFloorPlanAsync(floorPlanId);
            return ObjectMapper.Map<List<FloorPlanBooth>, List<FloorPlanBoothDto>>(booths);
        }

        [Authorize(MPPermissions.FloorPlans.Design)]
        [HttpPost("/api/app/floor-plan/{floorPlanId}/booths")]
        public async Task<FloorPlanBoothDto> AddBoothAsync(Guid floorPlanId, CreateFloorPlanBoothDto input)
        {
            // Verify floor plan and booth exist
            await _floorPlanRepository.GetAsync(floorPlanId);
            await _boothRepository.GetAsync(input.BoothId);

            // Check if booth is already on this floor plan
            var existing = await _floorPlanBoothRepository.FindByFloorPlanAndBoothAsync(floorPlanId, input.BoothId);
            if (existing != null)
            {
                throw new BusinessException("BOOTH_ALREADY_ON_FLOOR_PLAN");
            }

            var floorPlanBooth = new FloorPlanBooth(
                GuidGenerator.Create(),
                floorPlanId,
                input.BoothId,
                input.X,
                input.Y,
                input.Width,
                input.Height,
                input.Rotation);

            await _floorPlanBoothRepository.InsertAsync(floorPlanBooth);

            return ObjectMapper.Map<FloorPlanBooth, FloorPlanBoothDto>(floorPlanBooth);
        }

        [Authorize(MPPermissions.FloorPlans.Design)]
        [HttpPut("/api/app/floor-plan/{floorPlanId}/booths/{boothId}")]
        public async Task<FloorPlanBoothDto> UpdateBoothPositionAsync(Guid floorPlanId, Guid boothId, CreateFloorPlanBoothDto input)
        {
            var floorPlanBooth = await _floorPlanBoothRepository.FindByFloorPlanAndBoothAsync(floorPlanId, boothId);
            if (floorPlanBooth == null)
            {
                throw new BusinessException("BOOTH_NOT_FOUND_ON_FLOOR_PLAN");
            }

            floorPlanBooth.UpdatePosition(input.X, input.Y, input.Width, input.Height, input.Rotation);
            await _floorPlanBoothRepository.UpdateAsync(floorPlanBooth);

            return ObjectMapper.Map<FloorPlanBooth, FloorPlanBoothDto>(floorPlanBooth);
        }

        [Authorize(MPPermissions.FloorPlans.Design)]
        [HttpDelete("/api/app/floor-plan/{floorPlanId}/booths/{boothId}")]
        public async Task RemoveBoothAsync(Guid floorPlanId, Guid boothId)
        {
            var floorPlanBooth = await _floorPlanBoothRepository.FindByFloorPlanAndBoothAsync(floorPlanId, boothId);
            if (floorPlanBooth != null)
            {
                await _floorPlanBoothRepository.DeleteAsync(floorPlanBooth);
            }
        }

        [HttpGet("/api/app/floor-plan/{floorPlanId}/booths-availability")]
        public async Task<List<BoothAvailabilityDto>> GetBoothsAvailabilityAsync(
            Guid floorPlanId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            // Verify floor plan exists
            await _floorPlanRepository.GetAsync(floorPlanId);

            // Get all booths on this floor plan with booth details
            var floorPlanBooths = await _floorPlanBoothRepository.GetListByFloorPlanAsync(floorPlanId);
            var boothIds = floorPlanBooths.Select(fpb => fpb.BoothId).ToList();

            // Get all booths for this tenant
            var booths = await (await _boothRepository.GetQueryableAsync())
                .Where(b => boothIds.Contains(b.Id) && b.TenantId == CurrentTenant.Id)
                .ToListAsync();

            // Get all rentals that overlap with the period for these booths
            var rentalsInPeriod = await (await _rentalRepository.GetQueryableAsync())
                .Where(r =>
                    boothIds.Contains(r.BoothId) &&
                    r.TenantId == CurrentTenant.Id &&
                    r.Period.StartDate <= endDate &&
                    r.Period.EndDate >= startDate)
                .ToListAsync();

            // Get ALL future rentals (from today onwards) to calculate NextAvailableFrom correctly
            var today = DateTime.Today;
            var allFutureRentals = await (await _rentalRepository.GetQueryableAsync())
                .Where(r =>
                    boothIds.Contains(r.BoothId) &&
                    r.TenantId == CurrentTenant.Id &&
                    r.Period.EndDate >= today &&
                    (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended || r.Status == RentalStatus.Draft))
                .ToListAsync();

            var result = new List<BoothAvailabilityDto>();

            foreach (var booth in booths)
            {
                // Find all overlapping rentals for this booth (in the selected period)
                var overlappingRentals = rentalsInPeriod
                    .Where(r => r.BoothId == booth.Id)
                    .OrderBy(r => r.Period.StartDate)
                    .ToList();

                // Find all future rentals for this booth (for NextAvailableFrom calculation)
                var futureRentalsForBooth = allFutureRentals
                    .Where(r => r.BoothId == booth.Id)
                    .OrderBy(r => r.Period.StartDate)
                    .ToList();

                // Determine status based on priority
                var status = DetermineBoothStatus(booth, overlappingRentals, startDate, endDate);

                // Find overlaps
                var overlaps = overlappingRentals
                    .Select(r => new RentalOverlapDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        StartDate = r.Period.StartDate,
                        EndDate = r.Period.EndDate,
                        Status = r.Status.ToString()
                    })
                    .ToList();

                // Calculate NextAvailableFrom using ALL future rentals
                var nextAvailableFrom = CalculateNextAvailableFrom(
                    booth,
                    futureRentalsForBooth,
                    today);

                result.Add(new BoothAvailabilityDto
                {
                    BoothId = booth.Id,
                    BoothNumber = booth.Number,
                    Status = status,
                    NextAvailableFrom = nextAvailableFrom,
                    Overlaps = overlaps
                });
            }

            return result;
        }

        private string DetermineBoothStatus(
            Domain.Booths.Booth booth,
            List<Rental> overlappingRentals,
            DateTime startDate,
            DateTime endDate)
        {
            // Check booth physical status first
            if (booth.Status == Domain.Booths.BoothStatus.Maintenance)
            {
                return "maintenance";
            }

            // Check for active rentals (Active or Extended)
            var hasActiveRental = overlappingRentals.Any(r =>
                r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended);

            if (hasActiveRental)
            {
                return "rented";
            }

            // Check for pending/draft reservations
            var hasPendingReservation = overlappingRentals.Any(r => r.Status == RentalStatus.Draft);

            if (hasPendingReservation)
            {
                return "reserved";
            }

            return "available";
        }

        private DateTime CalculateNextAvailableFrom(
            Domain.Booths.Booth booth,
            List<Rental> futureRentals,
            DateTime referenceDate)
        {
            // If booth is in maintenance, next available is unknown (return far future)
            if (booth.Status == Domain.Booths.BoothStatus.Maintenance)
            {
                return referenceDate.AddYears(10);
            }

            // If no future rentals, available from today (or reference date)
            if (!futureRentals.Any())
            {
                return referenceDate;
            }

            // Sort rentals by start date
            var sortedRentals = futureRentals.OrderBy(r => r.Period.StartDate).ToList();

            // Check if there's a gap at the beginning (before first rental)
            var firstRental = sortedRentals.First();
            if (firstRental.Period.StartDate > referenceDate)
            {
                // Available from today until first rental starts
                return referenceDate;
            }

            // Find gaps between consecutive rentals
            DateTime currentEnd = referenceDate;
            foreach (var rental in sortedRentals)
            {
                // If there's a gap between current end and this rental's start
                if (rental.Period.StartDate > currentEnd.AddDays(1))
                {
                    // Found a gap - available from currentEnd + 1 day
                    return currentEnd.AddDays(1);
                }

                // Update current end to the maximum of current end and this rental's end
                if (rental.Period.EndDate > currentEnd)
                {
                    currentEnd = rental.Period.EndDate;
                }
            }

            // No gaps found - available after the last rental
            return currentEnd.AddDays(1);
        }

        private async Task InvalidateCacheAsync(Guid floorPlanId, Guid? tenantId)
        {
            // Invalidate specific floor plan cache
            await _floorPlanCache.RemoveAsync($"FloorPlan_{floorPlanId}");

            // Invalidate published floor plans list cache for tenant
            await _floorPlanListCache.RemoveAsync($"FloorPlans_Published_Tenant_{tenantId}");
        }
    }
}