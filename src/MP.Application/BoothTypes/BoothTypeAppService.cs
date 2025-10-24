using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MP.Application.Contracts.BoothTypes;
using MP.Domain.BoothTypes;
using MP.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;

namespace MP.Application.BoothTypes
{
    [Authorize(MPPermissions.BoothTypes.Default)]
    public class BoothTypeAppService : CrudAppService<
        BoothType,
        BoothTypeDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateBoothTypeDto,
        UpdateBoothTypeDto>, IBoothTypeAppService
    {
        private readonly BoothTypeManager _boothTypeManager;
        private readonly IBoothTypeRepository _boothTypeRepository;
        private readonly IDistributedCache<List<BoothTypeDto>> _cache;

        public BoothTypeAppService(
            IRepository<BoothType, Guid> repository,
            BoothTypeManager boothTypeManager,
            IBoothTypeRepository boothTypeRepository,
            IDistributedCache<List<BoothTypeDto>> cache) : base(repository)
        {
            _boothTypeManager = boothTypeManager;
            _boothTypeRepository = boothTypeRepository;
            _cache = cache;

            GetPolicyName = MPPermissions.BoothTypes.Default;
            GetListPolicyName = MPPermissions.BoothTypes.Default;
            CreatePolicyName = MPPermissions.BoothTypes.Create;
            UpdatePolicyName = MPPermissions.BoothTypes.Edit;
            DeletePolicyName = MPPermissions.BoothTypes.Delete;
        }

        public async Task<List<BoothTypeDto>> GetActiveTypesAsync()
        {
            var cacheKey = $"BoothTypes_Active_Tenant_{CurrentTenant?.Id}";

            var cachedData = await _cache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var activeTypes = await _boothTypeRepository.GetActiveTypesAsync();
                    return ObjectMapper.Map<List<BoothType>, List<BoothTypeDto>>(activeTypes);
                },
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                }
            );

            return cachedData;
        }

        [Authorize(MPPermissions.BoothTypes.ManageTypes)]
        public override async Task<BoothTypeDto> CreateAsync(CreateBoothTypeDto input)
        {
            var boothType = await _boothTypeManager.CreateAsync(
                input.Name,
                input.Description,
                input.CommissionPercentage,
                Guid.Empty, // TODO: Get organizationalUnitId from user context or input
                CurrentTenant.Id);

            var savedBoothType = await Repository.InsertAsync(boothType);

            await InvalidateCacheAsync();

            return ObjectMapper.Map<BoothType, BoothTypeDto>(savedBoothType);
        }

        [Authorize(MPPermissions.BoothTypes.ManageTypes)]
        public override async Task<BoothTypeDto> UpdateAsync(Guid id, UpdateBoothTypeDto input)
        {
            var boothType = await Repository.GetAsync(id);

            await _boothTypeManager.UpdateAsync(
                boothType,
                input.Name,
                input.Description,
                input.CommissionPercentage);

            var updatedBoothType = await Repository.UpdateAsync(boothType);

            await InvalidateCacheAsync();

            return ObjectMapper.Map<BoothType, BoothTypeDto>(updatedBoothType);
        }

        [Authorize(MPPermissions.BoothTypes.ManageTypes)]
        public async Task<BoothTypeDto> ActivateAsync(Guid id)
        {
            var boothType = await Repository.GetAsync(id);
            boothType.Activate();
            var updatedBoothType = await Repository.UpdateAsync(boothType);

            await InvalidateCacheAsync();

            return ObjectMapper.Map<BoothType, BoothTypeDto>(updatedBoothType);
        }

        [Authorize(MPPermissions.BoothTypes.ManageTypes)]
        public async Task<BoothTypeDto> DeactivateAsync(Guid id)
        {
            var boothType = await Repository.GetAsync(id);
            boothType.Deactivate();
            var updatedBoothType = await Repository.UpdateAsync(boothType);

            await InvalidateCacheAsync();

            return ObjectMapper.Map<BoothType, BoothTypeDto>(updatedBoothType);
        }

        private async Task InvalidateCacheAsync()
        {
            var cacheKey = $"BoothTypes_Active_Tenant_{CurrentTenant?.Id}";
            await _cache.RemoveAsync(cacheKey);
        }
    }
}