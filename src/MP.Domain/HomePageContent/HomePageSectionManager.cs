using System;
using System.Threading.Tasks;
using MP.HomePageContent;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.HomePageContent
{
    public class HomePageSectionManager : DomainService
    {
        private readonly IHomePageSectionRepository _repository;
        private readonly ICurrentTenant _currentTenant;

        public HomePageSectionManager(
            IHomePageSectionRepository repository,
            ICurrentTenant currentTenant)
        {
            _repository = repository;
            _currentTenant = currentTenant;
        }

        public async Task<HomePageSection> CreateAsync(
            HomePageSectionType sectionType,
            string title,
            Guid organizationalUnitId,
            string? subtitle = null,
            string? content = null,
            Guid? imageFileId = null,
            string? linkUrl = null,
            string? linkText = null,
            DateTime? validFrom = null,
            DateTime? validTo = null,
            string? backgroundColor = null,
            string? textColor = null)
        {
            // Get next order number
            var maxOrder = await _repository.GetMaxOrderAsync();
            var order = maxOrder + 1;

            var section = new HomePageSection(
                GuidGenerator.Create(),
                sectionType,
                title,
                order,
                organizationalUnitId,
                _currentTenant.Id
            );

            // Set optional properties
            if (!string.IsNullOrWhiteSpace(subtitle))
                section.SetSubtitle(subtitle);

            if (!string.IsNullOrWhiteSpace(content))
                section.SetContent(content);

            if (imageFileId.HasValue)
                section.SetImageFileId(imageFileId);

            if (!string.IsNullOrWhiteSpace(linkUrl) || !string.IsNullOrWhiteSpace(linkText))
                section.SetLink(linkUrl, linkText);

            if (validFrom.HasValue || validTo.HasValue)
                section.SetValidityPeriod(validFrom, validTo);

            if (!string.IsNullOrWhiteSpace(backgroundColor) || !string.IsNullOrWhiteSpace(textColor))
                section.SetColors(backgroundColor, textColor);

            return await _repository.InsertAsync(section);
        }

        public async Task<HomePageSection> UpdateAsync(
            HomePageSection section,
            HomePageSectionType sectionType,
            string title,
            string? subtitle = null,
            string? content = null,
            Guid? imageFileId = null,
            string? linkUrl = null,
            string? linkText = null,
            DateTime? validFrom = null,
            DateTime? validTo = null,
            string? backgroundColor = null,
            string? textColor = null)
        {
            section.SetSectionType(sectionType);
            section.SetTitle(title);
            section.SetSubtitle(subtitle);
            section.SetContent(content);
            section.SetImageFileId(imageFileId);
            section.SetLink(linkUrl, linkText);
            section.SetValidityPeriod(validFrom, validTo);
            section.SetColors(backgroundColor, textColor);

            return await _repository.UpdateAsync(section);
        }

        public async Task DeleteAsync(Guid id)
        {
            var section = await _repository.GetAsync(id);
            await _repository.DeleteAsync(section);
        }

        public async Task<HomePageSection> ActivateAsync(Guid id)
        {
            var section = await _repository.GetAsync(id);
            section.Activate();
            return await _repository.UpdateAsync(section);
        }

        public async Task<HomePageSection> DeactivateAsync(Guid id)
        {
            var section = await _repository.GetAsync(id);
            section.Deactivate();
            return await _repository.UpdateAsync(section);
        }

        public async Task ReorderAsync(Guid id, int newOrder)
        {
            if (newOrder < 0)
                throw new BusinessException("HOMEPAGE_SECTION_ORDER_CANNOT_BE_NEGATIVE");

            var section = await _repository.GetAsync(id);
            section.SetOrder(newOrder);
            await _repository.UpdateAsync(section);
        }
    }
}
