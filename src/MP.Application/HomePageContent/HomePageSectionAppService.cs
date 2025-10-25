using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Authorization;
using MP.Application.Contracts.HomePageContent;
using MP.Domain.HomePageContent;
using MP.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using MP.Domain.OrganizationalUnits;
using Volo.Abp;

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
        private readonly ICurrentOrganizationalUnit _currentOrganizationalUnit;

        public HomePageSectionAppService(
            IHomePageSectionRepository repository,
            HomePageSectionManager manager,
            ICurrentOrganizationalUnit currentOrganizationalUnit)
            : base(repository)
        {
            _repository = repository;
            _manager = manager;
            _currentOrganizationalUnit = currentOrganizationalUnit;

            GetPolicyName = MPPermissions.HomePageContent.Default;
            GetListPolicyName = MPPermissions.HomePageContent.Default;
            CreatePolicyName = MPPermissions.HomePageContent.Create;
            UpdatePolicyName = MPPermissions.HomePageContent.Edit;
            DeletePolicyName = MPPermissions.HomePageContent.Delete;
        }

        [Authorize(MPPermissions.HomePageContent.Create)]
        public override async Task<HomePageSectionDto> CreateAsync(CreateHomePageSectionDto input)
        {
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED")
                .WithData("message", "Current organizational unit context is not set");

            var section = await _manager.CreateAsync(
                input.SectionType,
                input.Title,
                organizationalUnitId,
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
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED");

            if (section.OrganizationalUnitId != organizationalUnitId)
            {
                throw new AbpAuthorizationException("You do not have permission to access this section");
            }

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
            var section = await _repository.GetAsync(id);
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED");

            if (section.OrganizationalUnitId != organizationalUnitId)
            {
                throw new AbpAuthorizationException("You do not have permission to access this section");
            }

            await _manager.DeleteAsync(id);
        }

        [Authorize(MPPermissions.HomePageContent.Default)]
        public async Task<List<HomePageSectionDto>> GetAllOrderedAsync()
        {
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED");
            var sections = await _repository.GetAllOrderedAsync();

            // Filter by current organizational unit
            var unitSections = sections.Where(s => s.OrganizationalUnitId == organizationalUnitId).ToList();
            return ObjectMapper.Map<List<Domain.HomePageContent.HomePageSection>, List<HomePageSectionDto>>(unitSections);
        }

        [AllowAnonymous] // Public endpoint for displaying on homepage
        public async Task<List<HomePageSectionDto>> GetActiveForDisplayAsync()
        {
            var organizationalUnitId = _currentOrganizationalUnit.Id;
            var sections = await _repository.GetActiveOrderedAsync();

            // Filter by validity period and organizational unit (if context available)
            var validSections = sections
                .Where(s => s.IsValidForDisplay() && (organizationalUnitId == null || s.OrganizationalUnitId == organizationalUnitId))
                .ToList();

            return ObjectMapper.Map<List<Domain.HomePageContent.HomePageSection>, List<HomePageSectionDto>>(validSections);
        }

        [Authorize(MPPermissions.HomePageContent.Manage)]
        public async Task<HomePageSectionDto> ActivateAsync(Guid id)
        {
            var section = await _repository.GetAsync(id);
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED");

            if (section.OrganizationalUnitId != organizationalUnitId)
            {
                throw new AbpAuthorizationException("You do not have permission to access this section");
            }

            var activatedSection = await _manager.ActivateAsync(id);
            return ObjectMapper.Map<Domain.HomePageContent.HomePageSection, HomePageSectionDto>(activatedSection);
        }

        [Authorize(MPPermissions.HomePageContent.Manage)]
        public async Task<HomePageSectionDto> DeactivateAsync(Guid id)
        {
            var section = await _repository.GetAsync(id);
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED");

            if (section.OrganizationalUnitId != organizationalUnitId)
            {
                throw new AbpAuthorizationException("You do not have permission to access this section");
            }

            var deactivatedSection = await _manager.DeactivateAsync(id);
            return ObjectMapper.Map<Domain.HomePageContent.HomePageSection, HomePageSectionDto>(deactivatedSection);
        }

        [Authorize(MPPermissions.HomePageContent.Manage)]
        public async Task ReorderAsync(List<ReorderSectionDto> reorderList)
        {
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED");

            foreach (var item in reorderList)
            {
                var section = await _repository.GetAsync(item.Id);

                if (section.OrganizationalUnitId != organizationalUnitId)
                {
                    throw new AbpAuthorizationException("You do not have permission to access this section");
                }

                await _manager.ReorderAsync(item.Id, item.Order);
            }
        }
    }
}
