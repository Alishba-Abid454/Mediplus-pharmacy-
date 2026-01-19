// Hubs/OrderHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace _10PercentWebProject.Hubs
{
    public class OrderHub : Hub
    {
        // Most important functions only:

        // 1. Send new order notification to admins
        public async Task SendNewOrderNotification(int orderId, string customerName, decimal totalAmount)
        {
            await Clients.Group("Admins").SendAsync("ReceiveNewOrderNotification",
                orderId, customerName, totalAmount);
        }

        // 2. Send order status update to all
        public async Task SendOrderStatusUpdate(int orderId, string newStatus)
        {
            await Clients.All.SendAsync("ReceiveOrderStatusUpdate", orderId, newStatus);
        }

        // 3. Join admin group
        public async Task JoinOrderAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        // 4. Join customer group
        public async Task JoinCustomerGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
    }
}