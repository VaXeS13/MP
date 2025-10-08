using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace MP.Domain.BoothTypes
{
    public class BoothTypeManager : DomainService
    {
        private readonly IBoothTypeRepository _boothTypeRepository;

        public BoothTypeManager(IBoothTypeRepository boothTypeRepository)
        {
            _boothTypeRepository = boothTypeRepository;
        }

        public async Task<BoothType> CreateAsync(
            string name,
            string description,
            decimal commissionPercentage,
            Guid? tenantId = null)
        {
            await ValidateNameUniqueAsync(name);

            var id = GuidGenerator.Create();
            return new BoothType(id, name, description, commissionPercentage, tenantId);
        }

        public async Task UpdateAsync(
            BoothType boothType,
            string name,
            string description,
            decimal commissionPercentage)
        {
            if (boothType.Name != name)
            {
                await ValidateNameUniqueAsync(name, boothType.Id);
            }

            boothType.SetName(name);
            boothType.SetDescription(description);
            boothType.SetCommissionPercentage(commissionPercentage);
        }

        private async Task ValidateNameUniqueAsync(string name, Guid? excludeId = null)
        {
            var isUnique = await _boothTypeRepository.IsNameUniqueAsync(name, excludeId);
            if (!isUnique)
            {
                throw new BusinessException("BOOTH_TYPE_NAME_ALREADY_EXISTS")
                    .WithData("name", name);
            }
        }
    }
}