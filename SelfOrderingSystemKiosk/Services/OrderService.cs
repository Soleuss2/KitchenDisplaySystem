using MongoDB.Driver;
using SelfOrderingSystemKiosk.Areas.Customer.Models;


namespace SelfOrderingSystemKiosk.Services
{
    public class OrderService
    {
        private readonly IMongoCollection<Order> _orders;

        public OrderService(Models.KitchenDatabase db)
        {
            _orders = db.Database.GetCollection<Order>("Orders");
        }

        public async Task<List<Order>> GetAllAsync() =>
            await _orders.Find(_ => true).ToListAsync();

        // Get order by ID
        public async Task<Order> GetByIdAsync(string id)
        {
            return await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        }

        // Get order by order number
        public async Task<Order> GetByOrderNumberAsync(string orderNumber)
        {
            return await _orders.Find(o => o.OrderNumber == orderNumber).FirstOrDefaultAsync();
        }

        // Create new order
        public async Task CreateAsync(Order order)
        {
            await _orders.InsertOneAsync(order);
        }

        // Update order
        public async Task UpdateAsync(string id, Order order)
        {
            await _orders.ReplaceOneAsync(o => o.Id == id, order);
        }

        // Update order status
        public async Task UpdateStatusAsync(string id, string status)
        {
            var update = Builders<Order>.Update.Set(o => o.Status, status);
            await _orders.UpdateOneAsync(o => o.Id == id, update);
        }

        // Delete order
        public async Task DeleteAsync(string id)
        {
            await _orders.DeleteOneAsync(o => o.Id == id);
        }

        // Get orders by status
        public async Task<List<Order>> GetByStatusAsync(string status)
        {
            return await _orders.Find(o => o.Status == status).ToListAsync();
        }

        // Get orders by date range
        public async Task<List<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _orders.Find(o => o.OrderDate >= startDate && o.OrderDate <= endDate).ToListAsync();
        }

        // Cancel order by ID
        public async Task CancelOrderAsync(string id)
        {
            var update = Builders<Order>.Update.Set(o => o.Status, "Canceled");
            await _orders.UpdateOneAsync(o => o.Id == id, update);
        }

        // Cancel order by order number
        public async Task CancelOrderByOrderNumberAsync(string orderNumber)
        {
            var update = Builders<Order>.Update.Set(o => o.Status, "Canceled");
            await _orders.UpdateOneAsync(o => o.OrderNumber == orderNumber, update);
        }

        // Get first order for a table (to track when 1-hour timer starts)
        public async Task<Order> GetFirstOrderByTableAsync(string tableNumber)
        {
            if (string.IsNullOrEmpty(tableNumber))
                return null;

            // Get the earliest order for this table that is not canceled
            var orders = await _orders
                .Find(o => o.TableNumber == tableNumber && 
                          o.Status != "Canceled" && 
                          o.DiningType == "DineIn")
                .SortBy(o => o.OrderDate)
                .Limit(1)
                .ToListAsync();

            return orders.FirstOrDefault();
        }

        // Get all orders for a table (for checking if table has any orders)
        public async Task<List<Order>> GetOrdersByTableAsync(string tableNumber)
        {
            if (string.IsNullOrEmpty(tableNumber))
                return new List<Order>();

            return await _orders
                .Find(o => o.TableNumber == tableNumber && 
                          o.Status != "Canceled" && 
                          o.DiningType == "DineIn")
                .SortBy(o => o.OrderDate)
                .ToListAsync();
        }
    }
}