// wwwroot/js/inventoryHub.js
var inventoryConnection = new signalR.HubConnectionBuilder().withUrl("/inventoryHub").build();

// Listen for inventory updates
inventoryConnection.on("ReceiveInventoryUpdate", function (medicineId, newStock, medicineName) {
    console.log("Stock updated: " + medicineName + " - New stock: " + newStock);

    // Update stock display if element exists
    var stockElement = document.querySelector('[data-medicine-id="' + medicineId + '"] .stock-count');
    if (stockElement) {
        stockElement.textContent = newStock;
    }

    // Show notification
    showNotification(medicineName + " stock updated to " + newStock);
});

// Listen for low stock alerts
inventoryConnection.on("ReceiveLowStockAlert", function (medicineId, currentStock, threshold, medicineName) {
    console.log("Low stock alert: " + medicineName);

    // Show alert notification
    showAlert("⚠️ Low stock: " + medicineName + " - Only " + currentStock + " left!");
});

// Connect to hub
inventoryConnection.start().then(function () {
    console.log("Connected to Inventory Hub");

    // Join admin group if user is admin
    if (window.userRole === 'Admin') {
        inventoryConnection.invoke("JoinAdminGroup").catch(function (err) {
            console.error(err.toString());
        });
    }
}).catch(function (err) {
    console.error(err.toString());
});

// Helper functions
function showNotification(message) {
    // Create simple notification
    var notification = document.createElement('div');
    notification.className = 'notification';
    notification.textContent = message;
    notification.style.cssText = 'position: fixed; top: 20px; right: 20px; background: green; color: white; padding: 10px; z-index: 1000;';

    document.body.appendChild(notification);
    setTimeout(function () {
        notification.remove();
    }, 3000);
}

function showAlert(message) {
    // Create alert notification
    var alert = document.createElement('div');
    alert.className = 'alert';
    alert.textContent = message;
    alert.style.cssText = 'position: fixed; top: 60px; right: 20px; background: orange; color: white; padding: 10px; z-index: 1000;';

    document.body.appendChild(alert);
    setTimeout(function () {
        alert.remove();
    }, 5000);
}