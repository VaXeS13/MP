using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MP.Application.Contracts.Rentals;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp;

namespace MP.Controllers
{
    [RemoteService(Name = "Default")]
    [Area("app")]
    [ControllerName("Label")]
    [Route("api/app/labels")]
    public class LabelController : AbpControllerBase
    {
        private readonly ILabelGeneratorService _labelGeneratorService;

        public LabelController(ILabelGeneratorService labelGeneratorService)
        {
            _labelGeneratorService = labelGeneratorService;
        }

        [HttpGet]
        [Route("rental-item/{rentalItemId}")]
        public async Task<IActionResult> GenerateItemLabelAsync(Guid rentalItemId)
        {
            var pdfBytes = await _labelGeneratorService.GenerateLabelPdfAsync(rentalItemId);

            return File(pdfBytes, "application/pdf", $"label-{rentalItemId}.pdf");
        }

        [HttpPost]
        [Route("multiple")]
        public async Task<IActionResult> GenerateMultipleLabelsAsync([FromBody] Guid[] rentalItemIds)
        {
            if (rentalItemIds == null || rentalItemIds.Length == 0)
            {
                return BadRequest("No rental item IDs provided");
            }

            var pdfBytes = await _labelGeneratorService.GenerateMultipleLabelsPdfAsync(rentalItemIds);

            return File(pdfBytes, "application/pdf", $"labels-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        }
    }
}