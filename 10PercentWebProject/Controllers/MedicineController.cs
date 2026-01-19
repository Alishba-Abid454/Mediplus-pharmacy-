using _10PercentWebProject.Hubs;
using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace _10PercentWebProject.Controllers
{
    public class MedicineController : Controller
    {
        private readonly IMedicineRepository _medicineRepo;
        private readonly IHubContext<InventoryHub> _inventoryHub;
        private readonly IHubContext<OrderHub> _orderHub;

        public MedicineController(IMedicineRepository medicineRepo,
                                 IHubContext<InventoryHub> inventoryHub,
                                 IHubContext<OrderHub> orderHub)
        {
            _medicineRepo = medicineRepo;
            _inventoryHub = inventoryHub;
            _orderHub = orderHub;
        }
        // Simple session check for user tracking
        private void TrackUserVisit()
        {
            if (HttpContext.Session.GetString("UserVisited") == null)
            {
                HttpContext.Session.SetString("UserVisited", "true");
                HttpContext.Session.SetString("FirstVisit", DateTime.Now.ToString());
            }
        }

        // Get or create user session ID
        private string GetUserSessionId()
        {
            var sessionId = HttpContext.Session.GetString("SessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
            }
            return sessionId;
        }

        [AllowAnonymous]
        public async Task<IActionResult> HomePage()
        {
            // Track user session
            TrackUserVisit();

            // Existing async code (NO CHANGES)
            var allMedicinesTask = _medicineRepo.GetAllMedicinesAsync();
            var featuredMedicinesTask = _medicineRepo.GetFeaturedMedicinesAsync();
            var saleMedicinesTask = _medicineRepo.GetOnSaleMedicinesAsync();
            var categoriesTask = _medicineRepo.GetAllCategoriesAsync();

            await Task.WhenAll(allMedicinesTask, featuredMedicinesTask, saleMedicinesTask, categoriesTask);

            int cartCount = _medicineRepo.GetCartItemCount();

            ViewBag.FeaturedMedicines = featuredMedicinesTask.Result;
            ViewBag.SaleMedicines = saleMedicinesTask.Result;
            ViewBag.Categories = categoriesTask.Result;
            ViewBag.CartCount = cartCount;

            return View(allMedicinesTask.Result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> MedicineDetail(int id)
        {
            // Track session
            TrackUserVisit();

            // Existing code (NO CHANGES)
            var medicineTask = _medicineRepo.GetMedicineByIdAsync(id);
            var cartCountTask = Task.FromResult(_medicineRepo.GetCartItemCount());

            await Task.WhenAll(medicineTask, cartCountTask);

            Medicine medicine = medicineTask.Result;

            if (medicine == null)
            {
                TempData["ErrorMessage"] = "Medicine not found!";
                return RedirectToAction("HomePage");
            }

            var relatedMedicines = await _medicineRepo.GetMedicinesByCategoryAsync(medicine.Category);

            ViewBag.RelatedMedicines = relatedMedicines;
            ViewBag.CartCount = cartCountTask.Result;

            return View(medicine);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Search(string query)
        {
            // Track session
            TrackUserVisit();

            // Existing code (NO CHANGES)
            List<Medicine> searchedMedicines;

            if (string.IsNullOrEmpty(query))
            {
                searchedMedicines = await _medicineRepo.GetAllMedicinesAsync();
            }
            else
            {
                searchedMedicines = await _medicineRepo.SearchMedicinesAsync(query);
            }

            ViewBag.SearchQuery = query;
            ViewBag.CartCount = _medicineRepo.GetCartItemCount();

            return View(searchedMedicines);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Category(string name)
        {
            // Track session
            TrackUserVisit();

            // Existing code (NO CHANGES)
            var categoryMedicines = await _medicineRepo.GetMedicinesByCategoryAsync(name);

            ViewBag.CategoryName = name;
            ViewBag.CartCount = _medicineRepo.GetCartItemCount();

            return View(categoryMedicines);
        }

        [Authorize(Policy = "UserPolicy")]
        public IActionResult Cart()
        {
            // Track session
            TrackUserVisit();

            // Show session info (optional)
            ViewBag.SessionId = GetUserSessionId();
            ViewBag.FirstVisit = HttpContext.Session.GetString("FirstVisit");

            // Existing code (NO CHANGES)
            List<CartItem> cartItems = _medicineRepo.GetCartItems();
            decimal cartTotal = _medicineRepo.GetCartTotal();

            ViewBag.CartTotal = cartTotal;
            ViewBag.CartCount = _medicineRepo.GetCartItemCount();

            return View(cartItems);
        }

        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> Payment(int orderId)
        {
            // Store order ID in session for security
            HttpContext.Session.SetInt32("CurrentOrderId", orderId);

            // Existing code (NO CHANGES)
            var order = await _medicineRepo.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found!";
                return RedirectToAction("Cart");
            }

            ViewBag.CartCount = _medicineRepo.GetCartItemCount();
            ViewBag.PaymentMethods = new List<string> { "Credit Card", "UPI", "Cash on Delivery" };

            return View(order);
        }
        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        public async Task<IActionResult> AddToCart(int medicineId, int quantity = 1)
        {
            try
            {
                // Store last added item in session
                HttpContext.Session.SetInt32("LastAddedMedicine", medicineId);
                HttpContext.Session.SetString("LastAddedTime", DateTime.Now.ToString());

                // Get medicine
                var medicine = await _medicineRepo.GetMedicineByIdAsync(medicineId);

                if (medicine == null)
                {
                    TempData["ErrorMessage"] = "Medicine not found!";
                    return RedirectToAction("HomePage");
                }

                if (medicine.StockStatus == "Out of Stock")
                {
                    TempData["ErrorMessage"] = "This medicine is out of stock!";
                    return RedirectToAction("HomePage");
                }

                // Add to cart
                _medicineRepo.AddToCart(medicineId, quantity);

                // Send notifications
                await _inventoryHub.Clients.All.SendAsync("SendInventoryUpdate",
                    medicineId, 0, medicine.Name);

                if (medicine.StockStatus == "Low Stock")
                {
                    await _inventoryHub.Clients.Group("Admins").SendAsync("SendLowStockAlert",
                        medicineId, 0, 10, medicine.Name);
                }

                // Success message with medicine name
                TempData["SuccessMessage"] = $"{medicine.Name} added to cart successfully!";

                // Update cart count for display
                ViewBag.CartCount = _medicineRepo.GetCartItemCount();

                return RedirectToAction("HomePage");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while adding to cart.";
                return RedirectToAction("HomePage");
            }
        }

        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            // Track removed items in session
            var removedItems = HttpContext.Session.GetString("RemovedItems") ?? "";
            removedItems += $"{cartItemId},";
            HttpContext.Session.SetString("RemovedItems", removedItems);

            // Existing code (NO CHANGES)
            var cartItems = _medicineRepo.GetCartItems();
            var cartItem = cartItems.FirstOrDefault(c => c.CartItemId == cartItemId);

            if (cartItem != null)
            {
                _medicineRepo.RemoveFromCart(cartItemId);
                TempData["SuccessMessage"] = "Item removed from cart";
            }

            return RedirectToAction("Cart");
        }

        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            // Verify session order ID matches
            var sessionOrderId = HttpContext.Session.GetInt32("CurrentOrderId");
            if (sessionOrderId != orderId)
            {
                TempData["ErrorMessage"] = "Order verification failed!";
                return RedirectToAction("HomePage");
            }

            // Clear session order data
            HttpContext.Session.Remove("CurrentOrderId");

            // Existing code (NO CHANGES)
            var order = await _medicineRepo.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                return RedirectToAction("HomePage");
            }

            ViewBag.Order = order;
            ViewBag.EstimatedDelivery = DateTime.Now.AddDays(5).ToString("MMMM dd, yyyy");
            ViewBag.CartCount = _medicineRepo.GetCartItemCount();

            return View(order);
        }

        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        public IActionResult UpdateCart(int cartItemId, int quantity)
        {
            // Store update time in session
            HttpContext.Session.SetString("CartLastUpdated", DateTime.Now.ToString());

            // Existing code (NO CHANGES)
            _medicineRepo.UpdateCartItem(cartItemId, quantity);
            TempData["SuccessMessage"] = "Cart updated!";
            return RedirectToAction("Cart");
        }

        [Authorize(Policy = "UserPolicy")]
        public IActionResult Checkout()
        {
            // Store checkout start time
            HttpContext.Session.SetString("CheckoutStarted", DateTime.Now.ToString());

            // Existing code (NO CHANGES)
            List<CartItem> cartItems = _medicineRepo.GetCartItems();

            if (cartItems.Count == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty!";
                return RedirectToAction("Cart");
            }

            decimal cartTotal = _medicineRepo.GetCartTotal();

            ViewBag.CartTotal = cartTotal;
            ViewBag.CartCount = _medicineRepo.GetCartItemCount();

            return View();
        }

        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(string address)
        {
            // Store address in session temporarily
            if (!string.IsNullOrEmpty(address))
            {
                HttpContext.Session.SetString("LastShippingAddress", address);
            }

            // Existing code (MINIMAL CHANGE)
            int userId = 1; // Get actual user ID from authentication

            if (string.IsNullOrEmpty(address))
            {
                // Try to use last address from session
                address = HttpContext.Session.GetString("LastShippingAddress");

                if (string.IsNullOrEmpty(address))
                {
                    TempData["ErrorMessage"] = "Please provide a delivery address!";
                    return RedirectToAction("Checkout");
                }
            }

            Order order = await _medicineRepo.CheckoutCartAsync(userId, address);

            if (order != null)
            {
                // Store order in session for quick access
                HttpContext.Session.SetInt32("LastOrderId", order.OrderId);

                await _orderHub.Clients.Group("Admins").SendAsync("SendNewOrderNotification",
                    order.OrderId, "Customer", order.TotalAmount);

                await _orderHub.Clients.All.SendAsync("SendOrderStatusUpdate",
                    order.OrderId, "Processing");

                TempData["SuccessMessage"] = $"Order #{order.OrderId} placed successfully!";
                return RedirectToAction("OrderConfirmation", new { id = order.OrderId });
            }

            TempData["ErrorMessage"] = "Failed to place order!";
            return RedirectToAction("Cart");
        }

        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int orderId, string paymentMethod)
        {
            // Verify session order
            var sessionOrderId = HttpContext.Session.GetInt32("CurrentOrderId");
            if (sessionOrderId != orderId)
            {
                TempData["ErrorMessage"] = "Invalid order session!";
                return RedirectToAction("Cart");
            }

            // Store payment method in session
            HttpContext.Session.SetString("LastPaymentMethod", paymentMethod);

            // Existing code (NO CHANGES)
            var order = await _medicineRepo.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found!";
                return RedirectToAction("Cart");
            }

            order.PaymentMethod = paymentMethod;
            bool paymentSuccess = await _medicineRepo.ProcessPaymentAsync(orderId, order.TotalAmount);

            if (paymentSuccess)
            {
                order.Status = "Paid";
                TempData["SuccessMessage"] = $"Payment successful! Order #{order.OrderId} confirmed.";
                return RedirectToAction("OrderSuccess", new { orderId = order.OrderId });
            }

            TempData["ErrorMessage"] = "Payment failed!";
            return RedirectToAction("Payment", new { orderId = orderId });
        }

        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        public IActionResult ClearCart()
        {
            // Track cart clears in session
            var clearCount = HttpContext.Session.GetInt32("CartClearCount") ?? 0;
            HttpContext.Session.SetInt32("CartClearCount", clearCount + 1);
            HttpContext.Session.SetString("LastCartClear", DateTime.Now.ToString());

            // Existing code (NO CHANGES)
            _medicineRepo.ClearCart();
            TempData["SuccessMessage"] = "Cart cleared successfully!";

            return RedirectToAction("Cart");
        }

        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            // Check if this order belongs to current session
            var lastOrderId = HttpContext.Session.GetInt32("LastOrderId");
            if (lastOrderId != id)
            {
                TempData["WarningMessage"] = "Viewing order from different session";
            }

            // Existing code (NO CHANGES)
            Order order = await _medicineRepo.GetOrderByIdAsync(id);

            if (order == null)
            {
                return RedirectToAction("HomePage");
            }

            ViewBag.CartCount = _medicineRepo.GetCartItemCount();
            return View(order);
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int medicineId, int newStock)
        {
            // Track admin actions in session
            var adminActions = HttpContext.Session.GetString("AdminStockUpdates") ?? "";
            adminActions += $"{medicineId}:{newStock},";
            HttpContext.Session.SetString("AdminStockUpdates", adminActions);

            // Existing code (NO CHANGES)
            var medicine = await _medicineRepo.GetMedicineByIdAsync(medicineId);

            if (medicine == null)
            {
                TempData["ErrorMessage"] = "Medicine not found!";
                return RedirectToAction("HomePage");
            }

            if (newStock == 0)
                medicine.StockStatus = "Out of Stock";
            else if (newStock < 10)
                medicine.StockStatus = "Low Stock";
            else
                medicine.StockStatus = "In Stock";

            await _inventoryHub.Clients.All.SendAsync("SendInventoryUpdate",
                medicineId, newStock, medicine.Name);

            if (newStock < 10)
            {
                await _inventoryHub.Clients.Group("Admins").SendAsync("SendLowStockAlert",
                    medicineId, newStock, 10, medicine.Name);
            }

            TempData["SuccessMessage"] = $"Stock updated for {medicine.Name}!";
            return RedirectToAction("MedicineDetail", new { id = medicineId });
        }

        // ============ NEW SESSION-RELATED ACTIONS ============
        [Authorize(Policy = "AdminPolicy")]

        public IActionResult ViewSessionInfo()
        {
            var sessionInfo = new
            {
                SessionId = GetUserSessionId(),
                FirstVisit = HttpContext.Session.GetString("FirstVisit"),
                LastAddedMedicine = HttpContext.Session.GetInt32("LastAddedMedicine"),
                LastAddedTime = HttpContext.Session.GetString("LastAddedTime"),
                CartClearCount = HttpContext.Session.GetInt32("CartClearCount"),
                LastCartClear = HttpContext.Session.GetString("LastCartClear"),
                LastOrderId = HttpContext.Session.GetInt32("LastOrderId"),
                LastPaymentMethod = HttpContext.Session.GetString("LastPaymentMethod")
            };

            return Json(sessionInfo);
        }
        [Authorize(Policy = "AdminPolicy")]

        public IActionResult ClearSessionData()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Session data cleared!";
            return RedirectToAction("HomePage");
        }
        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        public IActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                // Store update time in session
                HttpContext.Session.SetString("CartLastUpdated", DateTime.Now.ToString());

                // Update the cart item
                _medicineRepo.UpdateCartItem(cartItemId, quantity);

                // Get updated cart data
                List<CartItem> cartItems = _medicineRepo.GetCartItems();
                var updatedItem = cartItems.FirstOrDefault(c => c.CartItemId == cartItemId);

                if (updatedItem == null)
                {
                    return Json(new { success = false, message = "Item not found in cart" });
                }

                decimal cartTotal = _medicineRepo.GetCartTotal();

                return Json(new
                {
                    success = true,
                    itemTotal = updatedItem.Total,
                    cartTotal = cartTotal
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
