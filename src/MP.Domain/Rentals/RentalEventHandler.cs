using MP.Domain.Booths;
using MP.Domain.Rentals.Events;
using MP.Domain.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Microsoft.Extensions.Logging;

namespace MP.Domain.Rentals
{
    public class RentalEventHandler :
        ILocalEventHandler<RentalConfirmedEvent>,
        ILocalEventHandler<RentalCompletedEvent>,
        ILocalEventHandler<RentalCancelledEvent>,
        ITransientDependency
    {
        private readonly IBoothRepository _boothRepository;
        private readonly IRentalRepository _rentalRepository;
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<RentalEventHandler> _logger;

        public RentalEventHandler(
            IBoothRepository boothRepository,
            IRentalRepository rentalRepository,
            IItemRepository itemRepository,
            ILogger<RentalEventHandler> logger)
        {
            _boothRepository = boothRepository;
            _rentalRepository = rentalRepository;
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(RentalConfirmedEvent eventData)
        {
            // Gdy wynajęcie jest potwierdzone, oznacz stanowisko jako wynajęte
            var booth = await _boothRepository.GetAsync(eventData.Entity.BoothId);
            booth.MarkAsRented();
            await _boothRepository.UpdateAsync(booth);
        }

        public async Task HandleEventAsync(RentalCompletedEvent eventData)
        {
            // Gdy wynajęcie się kończy, zwolnij stanowisko
            var booth = await _boothRepository.GetAsync(eventData.Entity.BoothId);
            booth.MarkAsAvailable();
            await _boothRepository.UpdateAsync(booth);

            // Zwolnij przedmioty z arkuszy tego wynajmu
            await ReleaseItemsFromRentalAsync(eventData.Entity.Id);
        }

        public async Task HandleEventAsync(RentalCancelledEvent eventData)
        {
            // Gdy wynajęcie jest anulowane, zwolnij stanowisko
            var booth = await _boothRepository.GetAsync(eventData.Entity.BoothId);
            booth.MarkAsAvailable();
            await _boothRepository.UpdateAsync(booth);

            // Zwolnij przedmioty z arkuszy tego wynajmu
            await ReleaseItemsFromRentalAsync(eventData.Entity.Id);
        }

        /// <summary>
        /// Releases items from item sheets associated with a rental.
        /// Items that are not sold will be marked as available (Draft status) for reassignment.
        /// </summary>
        private async Task ReleaseItemsFromRentalAsync(Guid rentalId)
        {
            try
            {
                // Fetch rental with item sheets included
                var rental = await _rentalRepository.GetRentalWithItemsAsync(rentalId);

                if (rental == null)
                {
                    _logger.LogWarning("RentalEventHandler: Rental {RentalId} not found", rentalId);
                    return;
                }

                if (rental.ItemSheets == null || !rental.ItemSheets.Any())
                {
                    _logger.LogDebug("RentalEventHandler: No item sheets found for rental {RentalId}", rentalId);
                    return;
                }

                var itemsReleasedCount = 0;
                var itemsSoldCount = 0;

                foreach (var itemSheet in rental.ItemSheets)
                {
                    if (itemSheet.Items == null || !itemSheet.Items.Any())
                        continue;

                    foreach (var sheetItem in itemSheet.Items)
                    {
                        try
                        {
                            var item = await _itemRepository.GetAsync(sheetItem.ItemId);

                            // Only release items that are not sold
                            if (item.Status != ItemStatus.Sold)
                            {
                                item.MarkAsDraft();
                                await _itemRepository.UpdateAsync(item);
                                itemsReleasedCount++;
                                _logger.LogDebug("RentalEventHandler: Released item {ItemId} (status changed to Draft)", item.Id);
                            }
                            else
                            {
                                itemsSoldCount++;
                                _logger.LogDebug("RentalEventHandler: Item {ItemId} was sold, keeping status", item.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "RentalEventHandler: Error releasing item {ItemId} from rental {RentalId}",
                                sheetItem.ItemId, rental.Id);
                            // Continue processing other items even if one fails
                        }
                    }
                }

                _logger.LogInformation(
                    "RentalEventHandler: Completed releasing items for rental {RentalId}. Released: {ReleasedCount}, Sold: {SoldCount}",
                    rentalId, itemsReleasedCount, itemsSoldCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RentalEventHandler: Error releasing items from rental {RentalId}", rentalId);
            }
        }
    }
}
