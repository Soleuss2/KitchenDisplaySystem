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
    }
}