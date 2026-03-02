using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentationBenchmark.Services
{
    public interface IStockNotifier
    {
        Task NotifyLowStockAsync(string productId, int currentQuantity);
    }

    public class InventoryService : IStockNotifier
    {
        private readonly Dictionary<string, InventoryItem> _items;
        private readonly List<StockTransaction> _transactionLog;
        private readonly int _lowStockThreshold;

        public InventoryService(int lowStockThreshold = 10)
        {
            _items = new Dictionary<string, InventoryItem>(StringComparer.OrdinalIgnoreCase);
            _transactionLog = new List<StockTransaction>();
            _lowStockThreshold = lowStockThreshold > 0 ? lowStockThreshold : 10;
        }

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

        public IReadOnlyList<InventoryItem> GetLowStockItems()
        {
            return _items.Values
                .Where(i => i.Quantity <= _lowStockThreshold)
                .OrderBy(i => i.Quantity)
                .ThenBy(i => i.ProductId)
                .ToList()
                .AsReadOnly();
        }

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

        private async Task SendNotificationAsync(string productId, int quantity)
        {
            await Task.Delay(10);
        }

        public decimal CalculateInventoryValue(Func<string, decimal> priceResolver)
        {
            if (priceResolver is null)
                throw new ArgumentNullException(nameof(priceResolver));

            return _items.Values.Sum(item => item.Quantity * priceResolver(item.ProductId));
        }

        internal class InventoryItem
        {
            public string ProductId { get; set; }
            public int Quantity { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private class StockTransaction
        {
            public string ProductId { get; set; }
            public int Change { get; set; }
            public DateTime Timestamp { get; set; }
            public string RequestedBy { get; set; }
        }
    }
}
