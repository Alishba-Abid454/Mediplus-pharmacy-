using _10PercentWebProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _10PercentWebProject.Models.Interface
{
    public interface IAdminMedicineRepository
    {
        // Async methods with Task return type
        Task<List<AdminMedicine>> GetAllMedicinesAsync();
        Task<AdminMedicine> GetMedicineByIdAsync(int id);
        Task<int> AddMedicineAsync(AdminMedicine medicine);
        Task<bool> UpdateMedicineAsync(AdminMedicine medicine);
        Task<bool> DeleteMedicineAsync(int id);
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<List<AdminMedicine>> GetExpiringMedicinesAsync(int daysThreshold = 30);
        Task<List<AdminMedicine>> GetLowStockMedicinesAsync(int threshold = 10);
        Task<List<AdminMedicine>> SearchMedicinesAsync(string searchTerm, string category = null);
        Task<bool> UpdateStockAsync(int medicineId, int newQuantity);
        Task<bool> UpdateExpiryStatusAsync(int medicineId, DateTime newExpiryDate);
    }
}



/*using _10PercentWebProject.Models;
using System;
using System.Collections.Generic;

namespace _10PercentWebProject.Models.Interface
{
    public interface IAdminMedicineRepository
    {
        // Existing methods
        List<AdminMedicine> GetAllMedicines();
        AdminMedicine GetMedicineById(int id);
        int AddMedicine(AdminMedicine medicine);
        bool UpdateMedicine(AdminMedicine medicine);
        bool DeleteMedicine(int id);
        DashboardStats GetDashboardStats();
        List<AdminMedicine> GetExpiringMedicines(int daysThreshold = 30);
        List<AdminMedicine> GetLowStockMedicines(int threshold = 10);
        List<AdminMedicine> SearchMedicines(string searchTerm, string category = null);

        // Missing methods that need to be implemented
        bool UpdateStock(int medicineId, int newQuantity);
        bool UpdateExpiryStatus(int medicineId, DateTime newExpiryDate);
    }
}*/