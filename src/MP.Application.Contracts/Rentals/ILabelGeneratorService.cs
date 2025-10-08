using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.Rentals
{
    public interface ILabelGeneratorService : IApplicationService
    {
        Task<byte[]> GenerateLabelPdfAsync(Guid rentalItemId);
        Task<byte[]> GenerateMultipleLabelsPdfAsync(Guid[] rentalItemIds);
    }
}