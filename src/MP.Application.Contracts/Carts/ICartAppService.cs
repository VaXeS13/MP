using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Carts
{
    public interface ICartAppService : IApplicationService
    {
        /// <summary>
        /// Gets the current user's active cart
        /// </summary>
        Task<CartDto> GetMyCartAsync();

        /// <summary>
        /// Adds an item to the current user's cart
        /// </summary>
        Task<CartDto> AddItemAsync(AddToCartDto input);

        /// <summary>
        /// Updates an item in the cart
        /// </summary>
        Task<CartDto> UpdateItemAsync(Guid itemId, UpdateCartItemDto input);

        /// <summary>
        /// Removes an item from the cart
        /// </summary>
        Task<CartDto> RemoveItemAsync(Guid itemId);

        /// <summary>
        /// Clears all items from the cart
        /// </summary>
        Task<CartDto> ClearCartAsync();

        /// <summary>
        /// Checks out the cart - creates all rentals and initiates payment
        /// </summary>
        Task<CheckoutResultDto> CheckoutAsync(CheckoutCartDto input);
    }
}