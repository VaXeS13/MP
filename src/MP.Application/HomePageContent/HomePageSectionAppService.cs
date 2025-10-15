using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MP.Application.Contracts.HomePageContent;
using MP.Domain.HomePageContent;
using MP.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Application.HomePageContent
{
    [Authorize(MPPermissions.HomePageContent.Default)]
    public class HomePageSectionAppService :
        CrudAppService<
            Domain.HomePageContent.HomePageSection,
            HomePageSectionDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateHomePageSectionDto,
            UpdateHomePageSectionDto>,
        IHomePageSectionAppService
    {
        private readonly IHomePageSectionRepository _repository;
        private readonly HomePageSectionManager _manager;

        public HomePageSectionAppService(
            IHomePageSectionRepository repository,
            HomePageSectionManager manager)
            : base(repository)
        {
            _repository = repository;
            _manager = manager;

            GetPolicyName = MPPermissions.HomePageContent.Default;
            GetListPolicyName = MPPermissions.HomePageContent.Default;
            CreatePolicyName = MPPermissions.HomePageContent.Create;
            UpdatePolicyName = MPPermissions.HomePageContent.Edit;
            DeletePolicyName = MPPermissions.HomePageContent.Delete;
        }

        [Authorize(MPPermissions.HomePageContent.Create)]
        public override async Task<HomePageSectionDto> CreateAsync(CreateHomePageSectionDto input)
        {
            var section = await _manager.CreateAsync(
                input.SectionType,
                input.Title,
                input.Subtitle,
                input.Content,
                input.ImageFileId,
                input.LinkUrl,
                input.LinkText,
                input.ValidFrom,
                input.ValidTo,
                input.BackgroundColor,
                input.TextColor
            );

            return ObjectMapper.Map<Domain.HomePageContent.HomePageSection, HomePageSectionDto>(section);
        }

        [Authorize(MPPermissions.HomePageContent.Edit)]
        public override async Task<HomePageSectionDto> UpdateAsync(Guid id, UpdateHomePageSectionDto input)
        {
            var section = await _repository.GetAsync(id);

            section = await _manager.UpdateAsync(
                section,
                input.SectionType,
                input.Title,
                input.Subtitle,
                input.Content,
                input.ImageFileId,
                input.LinkUrl,
                input.LinkText,
                input.ValidFrom,
                input.ValidTo,
                input.BackgroundColor,
                input.TextColor
            );

            return ObjectMapper.Map<Domain.HomePageContent.HomePageSection, HomePageSectionDto>(section);
        }

        [Authorize(MPPermissions.HomePageContent.Delete)]
        public override async Task DeleteAsync(Guid id)
        {
            await _manager.DeleteAsync(id);
        }

        [Authorize(MPPermissions.HomePageContent.Default)]
        public async Task<List<HomePageSectionDto>> GetAllOrderedAsync()
        {
            var sections = await _repository.GetAllOrderedAsync();
            return ObjectMapper.Map<List<Domain.HomePageContent.HomePageSection>, List<HomePageSectionDto>>(sections);
        }

        [AllowAnonymous] // Public endpoint for displaying on homepage
        public async Task<List<HomePageSectionDto>> GetActiveForDisplayAsync()
        {
            var sections = await _repository.GetActiveOrderedAsync();

            // Filter by validity period
            var validSections = sections.Where(s => s.IsValidForDisplay()).ToList();

            return ObjectMapper.Map<List<Domain.HomePageContent.HomePageSection>, List<HomePageSectionDto>>(validSections);
        }

        [Authorize(MPPermissions.HomePageContent.Manage)]
        public async Task<HomePageSectionDto> ActivateAsync(Guid id)
        {
            var section = await _manager.ActivateAsync(id);
            return ObjectMapper.Map<Domain.HomePageContent.HomePageSection, HomePageSectionDto>(section);
        }

        [Authorize(MPPermissions.HomePageContent.Manage)]
        public async Task<HomePageSectionDto> DeactivateAsync(Guid id)
        {
            var section = await _manager.DeactivateAsync(id);
            return ObjectMapper.Map<Domain.HomePageContent.HomePageSection, HomePageSectionDto>(section);
        }

        [Authorize(MPPermissions.HomePageContent.Manage)]
        public async Task ReorderAsync(List<ReorderSectionDto> reorderList)
        {
            foreach (var item in reorderList)
            {
                await _manager.ReorderAsync(item.Id, item.Order);
            }
        }
    }
}
