// Hubs/InventoryHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace _10PercentWebProject.Hubs
{
    public class InventoryHub : Hub
    {
        // Send inventory updates to all clients
        public async Task SendInventoryUpdate(int medicineId, int newStock, string medicineName)
        {
            await Clients.All.SendAsync("ReceiveInventoryUpdate", medicineId, newStock, medicineName);
        }

        // Send low stock alert (admin only)
        public async Task SendLowStockAlert(int medicineId, int currentStock, int threshold, string medicineName)
        {
            await Clients.Group("Admins").SendAsync("ReceiveLowStockAlert",
                medicineId, currentStock, threshold, medicineName);
        }

        // Join admin group
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }
    }
}