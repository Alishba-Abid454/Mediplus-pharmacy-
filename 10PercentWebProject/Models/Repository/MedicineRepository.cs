using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace _10PercentWebProject.Repositories
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly string _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=MedicineDB;Trusted_Connection=True;";
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Orders (simple in-memory for now)
        private static List<Order> _orders = new List<Order>();
        private static int _nextOrderId = 1;

        public MedicineRepository(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // ============ SIMPLE SESSION CART ============

        // Session se cart lao
        private List<CartItem> GetCart()
        {
            if (_httpContextAccessor.HttpContext == null)
                return new List<CartItem>();

            var session = _httpContextAccessor.HttpContext.Session;
            var cartJson = session.GetString("Cart");

            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItem>();

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson);
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        // Session mein cart save karo
        private void SaveCart(List<CartItem> cart)
        {
            if (_httpContextAccessor.HttpContext == null)
                return;

            var session = _httpContextAccessor.HttpContext.Session;
            var cartJson = JsonSerializer.Serialize(cart);
            session.SetString("Cart", cartJson);
        }

        public void AddToCart(int medicineId, int quantity = 1)
        {
            // Medicine get karo (simple way)
            var medicine = GetMedicineByIdAsync(medicineId).GetAwaiter().GetResult();
            if (medicine == null) return;

            var cart = GetCart();

            // Check if already in cart
            var existing = cart.FirstOrDefault(x => x.MedicineId == medicineId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                // New cart item
                int newId = cart.Count > 0 ? cart.Max(x => x.CartItemId) + 1 : 1;

                cart.Add(new CartItem
                {
                    CartItemId = newId,
                    MedicineId = medicineId,
                    Name = medicine.Name,
                    Price = medicine.Price,
                    Quantity = quantity,
                    Category = medicine.Category
                });
            }

            SaveCart(cart);
        }

        public void RemoveFromCart(int cartItemId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.CartItemId == cartItemId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
        }

        public void UpdateCartItem(int cartItemId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.CartItemId == cartItemId);

            if (item != null && quantity > 0)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }
        }

        public List<CartItem> GetCartItems()
        {
            return GetCart();
        }

        public void ClearCart()
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Session.Remove("Cart");
            }
        }

        public int GetCartItemCount()
        {
            var cart = GetCart();
            return cart.Sum(x => x.Quantity);
        }

        public decimal GetCartTotal()
        {
            var cart = GetCart();
            return cart.Sum(x => x.Price * x.Quantity);
        }

        // ============ MEDICINE METHODS (SAME AS BEFORE) ============

        public async Task<List<Medicine>> GetAllMedicinesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT MedicineId, Name, Description, Category, 
                           Price, ImageUrl, StockStatus, IsFeatured,
                           IsOnSale, BadgeType
                    FROM Medicines 
                    WHERE IsActive = 1 
                    ORDER BY Name";

                var result = await connection.QueryAsync<Medicine>(sql);
                return result.ToList();
            }
        }

        public async Task<Medicine> GetMedicineByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT MedicineId, Name, Description, Category, 
                           Price, ImageUrl, StockStatus, IsFeatured,
                           IsOnSale, BadgeType
                    FROM Medicines 
                    WHERE MedicineId = @Id AND IsActive = 1";

                return await connection.QueryFirstOrDefaultAsync<Medicine>(
                    sql, new { Id = id });
            }
        }

        public async Task<List<Medicine>> SearchMedicinesAsync(string searchTerm)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT MedicineId, Name, Description, Category, 
                           Price, ImageUrl, StockStatus, IsFeatured,
                           IsOnSale, BadgeType
                    FROM Medicines 
                    WHERE IsActive = 1 
                    AND (Name LIKE @Search OR Description LIKE @Search)
                    ORDER BY Name";

                var result = await connection.QueryAsync<Medicine>(sql,
                    new { Search = $"%{searchTerm}%" });
                return result.ToList();
            }
        }

        public async Task<List<Medicine>> GetMedicinesByCategoryAsync(string category)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT MedicineId, Name, Description, Category, 
                           Price, ImageUrl, StockStatus, IsFeatured,
                           IsOnSale, BadgeType
                    FROM Medicines 
                    WHERE IsActive = 1 AND Category = @Category
                    ORDER BY Name";

                var result = await connection.QueryAsync<Medicine>(sql,
                    new { Category = category });
                return result.ToList();
            }
        }

        public async Task<List<Medicine>> GetFeaturedMedicinesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT TOP 8 MedicineId, Name, Description, Category, 
                           Price, ImageUrl, StockStatus, IsFeatured,
                           IsOnSale, BadgeType
                    FROM Medicines 
                    WHERE IsActive = 1 AND IsFeatured = 1
                    ORDER BY NEWID()";

                var result = await connection.QueryAsync<Medicine>(sql);
                return result.ToList();
            }
        }

        public async Task<List<Medicine>> GetOnSaleMedicinesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT TOP 6 MedicineId, Name, Description, Category, 
                           Price, ImageUrl, StockStatus, IsFeatured,
                           IsOnSale, BadgeType
                    FROM Medicines 
                    WHERE IsActive = 1 AND IsOnSale = 1
                    ORDER BY NEWID()";

                var result = await connection.QueryAsync<Medicine>(sql);
                return result.ToList();
            }
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = "SELECT DISTINCT Category FROM Medicines WHERE IsActive = 1";
                var result = await connection.QueryAsync<string>(sql);
                return result.ToList();
            }
        }

        // ============ ORDER METHODS ============

        public async Task<int> CreateOrderAsync(Order order)
        {
            order.OrderId = _nextOrderId++;
            order.OrderDate = System.DateTime.Now;
            _orders.Add(order);

            // Clear cart after order
            ClearCart();

            return await Task.FromResult(order.OrderId);
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            var order = _orders.FirstOrDefault(x => x.OrderId == orderId);
            return await Task.FromResult(order);
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            var orders = _orders.Where(x => x.UserId == userId).ToList();
            return await Task.FromResult(orders);
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null) return false;

            if (amount >= order.TotalAmount)
            {
                order.Status = "Paid";
                return true;
            }

            return false;
        }

        public async Task<Order> CheckoutCartAsync(int userId, string shippingAddress)
        {
            var cart = GetCart();
            if (cart.Count == 0) return null;

            var order = new Order
            {
                OrderId = _nextOrderId++,
                UserId = userId,
                OrderDate = System.DateTime.Now,
                Status = "Pending",
                ShippingAddress = shippingAddress,
                TotalAmount = GetCartTotal(),
                Items = cart.Select(x => new OrderItem
                {
                    MedicineId = x.MedicineId,
                    MedicineName = x.Name,
                    Price = x.Price,
                    Quantity = x.Quantity
                }).ToList()
            };

            _orders.Add(order);
            ClearCart();

            return await Task.FromResult(order);
        }
    }
}



/*using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _10PercentWebProject.Repositories
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly string _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=MedicineDB;Trusted_Connection=True;";

        // In-memory cart (REMAINS SYNCHRONOUS)
        private static List<CartItem> _cartItems = new List<CartItem>();
        private static List<Order> _orders = new List<Order>();
        private static int _nextCartItemId = 1;
        private static int _nextOrderId = 1;

        // ============ ASYNC MEDICINE VIEWING METHODS ============

        public async Task<List<Medicine>> GetAllMedicinesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType
                    FROM Medicines m
                    WHERE m.IsActive = 1 AND m.StockStatus != 'Out of Stock'
                    ORDER BY m.Name";

                var result = await connection.QueryAsync<Medicine>(sql);
                return result.ToList();
            }
        }

        public async Task<Medicine> GetMedicineByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType
                    FROM Medicines m
                    WHERE m.MedicineId = @Id 
                    AND m.IsActive = 1 
                    AND m.StockStatus != 'Out of Stock'";

                return await connection.QueryFirstOrDefaultAsync<Medicine>(
                    sql, new { Id = id });
            }
        }

        public async Task<List<Medicine>> SearchMedicinesAsync(string searchTerm)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType
                    FROM Medicines m
                    WHERE m.IsActive = 1 
                    AND m.StockStatus != 'Out of Stock'
                    AND (m.Name LIKE @SearchTerm 
                         OR m.Description LIKE @SearchTerm 
                         OR m.Category LIKE @SearchTerm)
                    ORDER BY m.Name";

                var result = await connection.QueryAsync<Medicine>(sql, new
                {
                    SearchTerm = $"%{searchTerm}%"
                });
                return result.ToList();
            }
        }

        public async Task<List<Medicine>> GetMedicinesByCategoryAsync(string category)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType
                    FROM Medicines m
                    WHERE m.IsActive = 1 
                    AND m.StockStatus != 'Out of Stock'
                    AND m.Category = @Category
                    ORDER BY m.Name";

                var result = await connection.QueryAsync<Medicine>(
                    sql, new { Category = category });
                return result.ToList();
            }
        }

        public async Task<List<Medicine>> GetFeaturedMedicinesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT TOP 8
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType
                    FROM Medicines m
                    WHERE m.IsActive = 1 
                    AND m.StockStatus != 'Out of Stock'
                    AND m.IsFeatured = 1
                    ORDER BY NEWID()";

                var result = await connection.QueryAsync<Medicine>(sql);
                return result.ToList();
            }
        }

        public async Task<List<Medicine>> GetOnSaleMedicinesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT TOP 6
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType
                    FROM Medicines m
                    WHERE m.IsActive = 1 
                    AND m.StockStatus != 'Out of Stock'
                    AND m.IsOnSale = 1
                    ORDER BY NEWID()";

                var result = await connection.QueryAsync<Medicine>(sql);
                return result.ToList();
            }
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT DISTINCT Category 
                    FROM Medicines 
                    WHERE IsActive = 1 
                    AND StockStatus != 'Out of Stock'
                    ORDER BY Category";

                var result = await connection.QueryAsync<string>(sql);
                return result.ToList();
            }
        }

        // ============ SYNCHRONOUS CART OPERATIONS (NO CHANGE) ============

        public void AddToCart(int medicineId, int quantity = 1)
        {
            var medicine = GetMedicineByIdAsync(medicineId).GetAwaiter().GetResult();
            if (medicine == null) return;

            var existingItem = _cartItems.FirstOrDefault(item => item.MedicineId == medicineId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _cartItems.Add(new CartItem
                {
                    CartItemId = _nextCartItemId++,
                    MedicineId = medicine.MedicineId,
                    Name = medicine.Name,
                    Description = medicine.Description,
                    Category = medicine.Category,
                    Price = medicine.Price,
                    Quantity = quantity,
                    Icon = GetMedicineIcon(medicine.Category),
                    PrescriptionType = medicine.Category == "Prescription" ? "prescription" : "otc",
                    PackageInfo = "Standard Package"
                });
            }
        }

        public void RemoveFromCart(int cartItemId)
        {
            var item = _cartItems.FirstOrDefault(i => i.CartItemId == cartItemId);
            if (item != null)
            {
                _cartItems.Remove(item);
            }
        }

        public void UpdateCartItem(int cartItemId, int quantity)
        {
            var item = _cartItems.FirstOrDefault(i => i.CartItemId == cartItemId);
            if (item != null)
            {
                item.Quantity = quantity > 0 ? quantity : 1;
            }
        }

        public List<CartItem> GetCartItems()
        {
            return _cartItems.ToList();
        }

        public void ClearCart()
        {
            _cartItems.Clear();
        }

        public int GetCartItemCount()
        {
            return _cartItems.Sum(item => item.Quantity);
        }

        public decimal GetCartTotal()
        {
            return _cartItems.Sum(item => item.Total);
        }

        private string GetMedicineIcon(string category)
        {
            return category.ToLower() switch
            {
                "tablets" or "capsules" => "fa-pills",
                "syrup" or "liquid" => "fa-prescription-bottle",
                "injection" => "fa-syringe",
                "ointment" => "fa-bottle-droplet",
                "vaccine" => "fa-shield-virus",
                _ => "fa-capsules"
            };
        }

        // ============ ASYNC ORDER OPERATIONS ============

        public async Task<int> CreateOrderAsync(Order order)
        {
            order.OrderId = _nextOrderId++;
            order.OrderDate = DateTime.Now;
            order.Status = "Pending";
            _orders.Add(order);

            ClearCart();
            return await Task.FromResult(order.OrderId);
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            return await Task.FromResult(_orders.FirstOrDefault(o => o.OrderId == orderId));
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            return await Task.FromResult(_orders.Where(o => o.UserId == userId).ToList());
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            var order = GetOrderByIdAsync(orderId).GetAwaiter().GetResult();
            if (order == null) return false;

            if (amount >= order.TotalAmount)
            {
                order.Status = "Paid";
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        public async Task<Order> CheckoutCartAsync(int userId, string shippingAddress)
        {
            var cartItems = GetCartItems();
            if (cartItems.Count == 0) return null;

            var order = new Order
            {
                OrderId = _nextOrderId++,
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                ShippingAddress = shippingAddress,
                PaymentMethod = "Credit Card",
                TotalAmount = GetCartTotal(),
                Items = cartItems.Select(item => new OrderItem
                {
                    MedicineId = item.MedicineId,
                    MedicineName = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity
                }).ToList()
            };

            _orders.Add(order);
            ClearCart();

            return await Task.FromResult(order);
        }
    }
}

*/