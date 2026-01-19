using _10PercentWebProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _10PercentWebProject.Models.Interface
{
    public interface IMedicineRepository
    {
        // Medicine Viewing Methods
        Task<List<Medicine>> GetAllMedicinesAsync();
        Task<Medicine> GetMedicineByIdAsync(int id);
        Task<List<Medicine>> SearchMedicinesAsync(string searchTerm);
        Task<List<Medicine>> GetMedicinesByCategoryAsync(string category);
        Task<List<Medicine>> GetFeaturedMedicinesAsync();
        Task<List<Medicine>> GetOnSaleMedicinesAsync();
        Task<List<string>> GetAllCategoriesAsync();

        // Cart Operations (These remain synchronous as they're in-memory)
        void AddToCart(int medicineId, int quantity = 1);
        void RemoveFromCart(int cartItemId);
        void UpdateCartItem(int cartItemId, int quantity);
        List<CartItem> GetCartItems();
        void ClearCart();
        int GetCartItemCount();
        decimal GetCartTotal();

        // Order Operations
        Task<int> CreateOrderAsync(Order order);
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetUserOrdersAsync(int userId);
        Task<bool> ProcessPaymentAsync(int orderId, decimal amount);
        Task<Order> CheckoutCartAsync(int userId, string shippingAddress);
    }
}


/*using _10PercentWebProject.Models;
using System.Collections.Generic;

namespace _10PercentWebProject.Models.Interface
{
    public interface IMedicineRepository
    {
        // Medicine Viewing
        List<Medicine> GetAllMedicines();
        Medicine GetMedicineById(int id);
        List<Medicine> SearchMedicines(string searchTerm);
        List<Medicine> GetMedicinesByCategory(string category);
        List<Medicine> GetFeaturedMedicines();
        List<Medicine> GetOnSaleMedicines();
        List<string> GetAllCategories();

        // Cart Operations
        void AddToCart(int medicineId, int quantity = 1);
        void RemoveFromCart(int cartItemId);
        void UpdateCartItem(int cartItemId, int quantity);
        List<CartItem> GetCartItems();
        void ClearCart();
        int GetCartItemCount();
        decimal GetCartTotal();
        Order CheckoutCart(int userId, string shippingAddress);

        // Order Operations
        Order GetOrderById(int orderId);
        List<Order> GetUserOrders(int userId);
        bool ProcessPayment(int orderId, decimal amount);
    }
}*/