using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentationBenchmark.Services
{
    /// <summary>
    /// Interface for notifying about low stock levels.
    /// </summary>
    public interface IStockNotifier
    {
        /// <summary>
        /// Notifies about low stock for a specific product.
        /// </summary>
        /// <param name="productId">The ID of the product with low stock.</param>
        /// <param name="currentQuantity">The current quantity of the product.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task NotifyLowStockAsync(string productId, int currentQuantity);
    }

    /// <summary>
    /// Service for managing inventory and stock levels.
    /// </summary>
    public class InventoryService : IStockNotifier
    {
        private readonly Dictionary<string, InventoryItem> _items;
        private readonly List<StockTransaction> _transactionLog;
        private readonly int _lowStockThreshold;

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryService"/> class.
        /// </summary>
        /// <param name="lowStockThreshold">The threshold for low stock notification.</param>
        public InventoryService(int lowStockThreshold = 10)
        {
            _items = new Dictionary<string, InventoryItem>(StringComparer.OrdinalIgnoreCase);
            _transactionLog = new List<StockTransaction>();
            _lowStockThreshold = lowStockThreshold > 0 ? lowStockThreshold : 10;
        }

        /// <summary>
        /// Adds stock for a specific product.
        /// </summary>
        /// <param name="productId">The ID of the product to add stock for.</param>
        /// <param name="quantity">The quantity of stock to add.</param>
        /// <param name="requestedBy">The identifier of the user requesting the stock addition.</param>
        /// <returns>A task that represents the asynchronous operation, with a value indicating whether the stock was added successfully.</returns>
        /// <exception cref="ArgumentException">Thrown when the product ID is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the quantity is not positive.</exception>
        public async Task<bool> AddStockAsync(string productId, int quantity, string requestedBy)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive");

            await Task.Delay(50); // Simulate database write

            lock (_items)
            {
                if (!_items.TryGetValue(productId, out var item))
                {
                    item = new InventoryItem { ProductId = productId, Quantity = 0 };
                    _items[productId] = item;
                }

                item.Quantity += quantity;
                item.LastUpdated = DateTime.UtcNow;
                
                _transactionLog.Add(new StockTransaction
                {
                    ProductId = productId,
                    Change = quantity,
                    Timestamp = DateTime.UtcNow,
                    RequestedBy = requestedBy ?? "system"
                });
            }

            return true;
        }

        /// <summary>
        /// Gets a read-only list of items that are low in stock.
        /// </summary>
        /// <returns>A read-only list of <see cref="InventoryItem"/> that are low in stock.</returns>
        public IReadOnlyList<InventoryItem> GetLowStockItems()
        {
            return _items.Values
                .Where(i => i.Quantity <= _lowStockThreshold)
                .OrderBy(i => i.Quantity)
                .ThenBy(i => i.ProductId)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Notifies about low stock for affected items.
        /// </summary>
        /// <param name="productId">The ID of the product that is low in stock.</param>
        /// <param name="currentQuantity">The current quantity of the product.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task NotifyLowStockAsync(string productId, int currentQuantity)
        {
            var tasks = new List<Task>();
            
            var affectedItems = _items.Values
                .Where(i => i.Quantity <= _lowStockThreshold && i.ProductId != productId)
                .Take(5);

            foreach (var item in affectedItems)
            {
                tasks.Add(SendNotificationAsync(item.ProductId, item.Quantity));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Sends a notification for low stock of a specific product.
        /// </summary>
        /// <param name="productId">The ID of the product to notify about.</param>
        /// <param name="quantity">The quantity of the product that is low in stock.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task SendNotificationAsync(string productId, int quantity)
        {
            await Task.Delay(10);
        }

        /// <summary>
        /// Calculates the total inventory value based on the provided price resolver.
        /// </summary>
        /// <param name="priceResolver">A function that resolves the price of a product given its ID.</param>
        /// <returns>The total inventory value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the price resolver is null.</exception>
        public decimal CalculateInventoryValue(Func<string, decimal> priceResolver)
        {
            if (priceResolver is null)
                throw new ArgumentNullException(nameof(priceResolver));

            return _items.Values.Sum(item => item.Quantity * priceResolver(item.ProductId));
        }

        /// <summary>
        /// Represents an inventory item.
        /// </summary>
        internal class InventoryItem
        {
            /// <summary>
            /// Gets or sets the product ID of the inventory item.
            /// </summary>
            public string ProductId { get; set; }

            /// <summary>
            /// Gets or sets the quantity of the inventory item.
            /// </summary>
            public int Quantity { get; set; }

            /// <summary>
            /// Gets or sets the last updated timestamp of the inventory item.
            /// </summary>
            public DateTime LastUpdated { get; set; }
        }

        /// <summary>
        /// Represents a stock transaction.
        /// </summary>
        private class StockTransaction
        {
            /// <summary>
            /// Gets or sets the product ID associated with the transaction.
            /// </summary>
            public string ProductId { get; set; }

            /// <summary>
            /// Gets or sets the change in quantity for the transaction.
            /// </summary>
            public int Change { get; set; }

            /// <summary>
            /// Gets or sets the timestamp of the transaction.
            /// </summary>
            public DateTime Timestamp { get; set; }

            /// <summary>
            /// Gets or sets the identifier of the user who requested the transaction.
            /// </summary>
            public string RequestedBy { get; set; }
        }
    }
}