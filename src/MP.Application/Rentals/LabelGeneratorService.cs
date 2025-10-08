using System;
using System.IO;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using QRCoder;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using MP.Domain.Items;
using MP.Application.Contracts.Rentals;
using SkiaSharp;

namespace MP.Application.Rentals
{
    public class LabelGeneratorService : ApplicationService, ILabelGeneratorService
    {
        private readonly IRepository<ItemSheetItem, Guid> _itemSheetItemRepository;
        private readonly IRepository<Item, Guid> _itemRepository;
        private readonly IRepository<ItemSheet, Guid> _itemSheetRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IRepository<Booth, Guid> _boothRepository;

        public LabelGeneratorService(
            IRepository<ItemSheetItem, Guid> itemSheetItemRepository,
            IRepository<Item, Guid> itemRepository,
            IRepository<ItemSheet, Guid> itemSheetRepository,
            IRepository<Rental, Guid> rentalRepository,
            IRepository<Booth, Guid> boothRepository)
        {
            _itemSheetItemRepository = itemSheetItemRepository;
            _itemRepository = itemRepository;
            _itemSheetRepository = itemSheetRepository;
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
        }

        public async Task<byte[]> GenerateLabelPdfAsync(Guid itemSheetItemId)
        {
            var itemSheetItem = await _itemSheetItemRepository.GetAsync(itemSheetItemId);
            var item = await _itemRepository.GetAsync(itemSheetItem.ItemId);
            var itemSheet = await _itemSheetRepository.GetAsync(itemSheetItem.ItemSheetId);

            if (!itemSheet.RentalId.HasValue)
                throw new Volo.Abp.BusinessException("ITEM_SHEET_NOT_ASSIGNED_TO_RENTAL");

            var rental = await _rentalRepository.GetAsync(itemSheet.RentalId.Value);
            var booth = await _boothRepository.GetAsync(rental.BoothId);

            return await GenerateLabelPdfAsync(itemSheetItem, item, rental, booth);
        }

        public async Task<byte[]> GenerateMultipleLabelsPdfAsync(Guid[] itemSheetItemIds)
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            document.Open();

            // 3x8 etykiet na stronie A4 (24 etykiety na stronę)
            const int labelsPerRow = 3;
            const int rowsPerPage = 8;
            const float labelWidth = 190f; // w punktach
            const float labelHeight = 100f;
            const float marginX = 10f;
            const float marginY = 10f;

            int currentLabel = 0;
            int currentRow = 0;

            foreach (var itemSheetItemId in itemSheetItemIds)
            {
                if (currentLabel >= labelsPerRow * rowsPerPage)
                {
                    document.NewPage();
                    currentLabel = 0;
                    currentRow = 0;
                }

                var itemSheetItem = await _itemSheetItemRepository.GetAsync(itemSheetItemId);
                var item = await _itemRepository.GetAsync(itemSheetItem.ItemId);
                var itemSheet = await _itemSheetRepository.GetAsync(itemSheetItem.ItemSheetId);

                if (!itemSheet.RentalId.HasValue)
                    continue; // Skip items not assigned to rental

                var rental = await _rentalRepository.GetAsync(itemSheet.RentalId.Value);
                var booth = await _boothRepository.GetAsync(rental.BoothId);

                float x = marginX + (currentLabel % labelsPerRow) * (labelWidth + marginX);
                float y = PageSize.A4.Height - marginY - (currentRow + 1) * (labelHeight + marginY);

                await AddLabelToDocumentAsync(document, writer, itemSheetItem, item, rental, booth, x, y, labelWidth, labelHeight);

                currentLabel++;
                if (currentLabel % labelsPerRow == 0)
                {
                    currentRow++;
                }
            }

            document.Close();
            return memoryStream.ToArray();
        }

        private async Task<byte[]> GenerateLabelPdfAsync(ItemSheetItem itemSheetItem, Item item, Rental rental, Booth booth)
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(new Rectangle(283f, 425f)); // rozmiar etykiety 10x15cm
            var writer = PdfWriter.GetInstance(document, memoryStream);
            document.Open();

            await AddLabelToDocumentAsync(document, writer, itemSheetItem, item, rental, booth, 0, 0, 283f, 425f);

            document.Close();
            return memoryStream.ToArray();
        }

        private async Task AddLabelToDocumentAsync(
            Document document,
            PdfWriter writer,
            ItemSheetItem itemSheetItem,
            Item item,
            Rental rental,
            Booth booth,
            float x,
            float y,
            float width,
            float height)
        {
            var contentByte = writer.DirectContent;

            // Ramka etykiety
            contentByte.Rectangle(x, y, width, height);
            contentByte.Stroke();

            // Fonts
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
            var priceFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);

            var currentY = y + height - 20;

            // Nagłówek z nazwą sklepu
            var headerPhrase = new Phrase("MARKETPLACE", titleFont);
            ColumnText.ShowTextAligned(contentByte, Element.ALIGN_CENTER, headerPhrase, x + width/2, currentY, 0);
            currentY -= 25;

            // Nazwa przedmiotu
            var itemNamePhrase = new Phrase(item.Name, titleFont);
            var itemNameColumn = new ColumnText(contentByte);
            itemNameColumn.SetSimpleColumn(x + 10, currentY - 30, x + width - 10, currentY);
            itemNameColumn.AddElement(new Paragraph(item.Name, titleFont));
            itemNameColumn.Go();
            currentY -= 35;

            // Kategoria
            if (!string.IsNullOrEmpty(item.Category))
            {
                var categoryPhrase = new Phrase($"Kategoria: {item.Category}", smallFont);
                ColumnText.ShowTextAligned(contentByte, Element.ALIGN_LEFT, categoryPhrase, x + 10, currentY, 0);
                currentY -= 15;
            }

            // Cena
            var priceText = item.Price.ToString("C");
            var pricePhrase = new Phrase(priceText, priceFont);
            ColumnText.ShowTextAligned(contentByte, Element.ALIGN_CENTER, pricePhrase, x + width/2, currentY, 0);
            currentY -= 30;

            // Numer stanowiska
            var boothPhrase = new Phrase($"Stanowisko: {booth.Number}", normalFont);
            ColumnText.ShowTextAligned(contentByte, Element.ALIGN_LEFT, boothPhrase, x + 10, currentY, 0);
            currentY -= 20;

            // Data dodania
            var datePhrase = new Phrase($"Data: {itemSheetItem.CreationTime:dd.MM.yyyy}", smallFont);
            ColumnText.ShowTextAligned(contentByte, Element.ALIGN_LEFT, datePhrase, x + 10, currentY, 0);
            currentY -= 30;

            // Barcode if available
            if (!string.IsNullOrEmpty(itemSheetItem.Barcode))
            {
                var barcodePhrase = new Phrase($"#{itemSheetItem.Barcode}", smallFont);
                ColumnText.ShowTextAligned(contentByte, Element.ALIGN_RIGHT, barcodePhrase, x + width - 10, y + 75, 0);
            }

            // ID przedmiotu
            var idPhrase = new Phrase($"ID: {itemSheetItem.Id.ToString().Substring(0, 8)}...", smallFont);
            ColumnText.ShowTextAligned(contentByte, Element.ALIGN_RIGHT, idPhrase, x + width - 10, y + 5, 0);
        }
    }
}