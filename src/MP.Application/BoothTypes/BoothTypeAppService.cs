using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MP.Application.Contracts.BoothTypes;
using MP.Domain.BoothTypes;
using MP.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
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

        public BoothTypeAppService(
            IRepository<BoothType, Guid> repository,
            BoothTypeManager boothTypeManager,
            IBoothTypeRepository boothTypeRepository) : base(repository)
        {
            _boothTypeManager = boothTypeManager;
            _boothTypeRepository = boothTypeRepository;

            GetPolicyName = MPPermissions.BoothTypes.Default;
            GetListPolicyName = MPPermissions.BoothTypes.Default;
            CreatePolicyName = MPPermissions.BoothTypes.Create;
            UpdatePolicyName = MPPermissions.BoothTypes.Edit;
            DeletePolicyName = MPPermissions.BoothTypes.Delete;
        }

        public async Task<List<BoothTypeDto>> GetActiveTypesAsync()
        {
            var activeTypes = await _boothTypeRepository.GetActiveTypesAsync();
            return ObjectMapper.Map<List<BoothType>, List<BoothTypeDto>>(activeTypes);
        }

        [Authorize(MPPermissions.BoothTypes.ManageTypes)]
        public override async Task<BoothTypeDto> CreateAsync(CreateBoothTypeDto input)
        {
            var boothType = await _boothTypeManager.CreateAsync(
                input.Name,
                input.Description,
                input.CommissionPercentage,
                CurrentTenant.Id);

            var savedBoothType = await Repository.InsertAsync(boothType);
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
            return ObjectMapper.Map<BoothType, BoothTypeDto>(updatedBoothType);
        }

        [Authorize(MPPermissions.BoothTypes.ManageTypes)]
        public async Task<BoothTypeDto> ActivateAsync(Guid id)
        {
            var boothType = await Repository.GetAsync(id);
            boothType.Activate();
            var updatedBoothType = await Repository.UpdateAsync(boothType);
            return ObjectMapper.Map<BoothType, BoothTypeDto>(updatedBoothType);
        }

        [Authorize(MPPermissions.BoothTypes.ManageTypes)]
        public async Task<BoothTypeDto> DeactivateAsync(Guid id)
        {
            var boothType = await Repository.GetAsync(id);
            boothType.Deactivate();
            var updatedBoothType = await Repository.UpdateAsync(boothType);
            return ObjectMapper.Map<BoothType, BoothTypeDto>(updatedBoothType);
        }
    }
}