var orderConnection = new signalR.HubConnectionBuilder().withUrl("/orderHub").build();

// Listen for new orders (admin only)
orderConnection.on("ReceiveNewOrderNotification", function (orderId, customerName, totalAmount) {
    if (window.userRole === 'Admin') {
        console.log(`New order #${orderId} from ${customerName} - $${totalAmount}`);
        showOrderNotification(`🛒 New Order #${orderId} from ${customerName}`, 'success');
    }
});

// Listen for order status updates
orderConnection.on("ReceiveOrderStatusUpdate", function (orderId, newStatus) {
    console.log(`Order #${orderId} status updated: ${newStatus}`);

    // Update status display if on order page
    var statusElement = document.querySelector(`[data-order-id="${orderId}"] .status`);
    if (statusElement) {
        statusElement.textContent = newStatus;
    }

    showOrderNotification(`Order #${orderId} status: ${newStatus}`, 'info');
});

// Listen for payment processed
orderConnection.on("ReceivePaymentProcessed", function (orderId, paymentMethod, amount) {
    console.log(`Payment processed for order #${orderId}: ${paymentMethod} - $${amount}`);
    showOrderNotification(`💳 Payment received for Order #${orderId}`, 'success');
});

// Connect to hub
orderConnection.start().then(function () {
    console.log("Connected to Order Hub");

    // Join appropriate groups
    if (window.userRole === 'Admin') {
        orderConnection.invoke("JoinOrderAdminGroup").catch(function (err) {
            console.error(err.toString());
        });
    } else if (window.userId && window.userId !== 'guest') {
        orderConnection.invoke("JoinCustomerGroup", window.userId).catch(function (err) {
            console.error(err.toString());
        });
    }
}).catch(function (err) {
    console.error(err.toString());
});

// Helper function to show order notifications
function showOrderNotification(message, type) {
    // Create notification element
    var notification = document.createElement('div');
    notification.className = 'order-notification';
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 80px;
        right: 20px;
        background: ${type === 'success' ? '#4CAF50' : '#2196F3'};
        color: white;
        padding: 10px 20px;
        border-radius: 5px;
        z-index: 1000;
        box-shadow: 0 2px 10px rgba(0,0,0,0.2);
    `;

    document.body.appendChild(notification);

    // Remove after 5 seconds
    setTimeout(function () {
        notification.remove();
    }, 5000);
}