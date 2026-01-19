using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _10PercentWebProject.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class AdminController : Controller
    {
        private readonly IAdminMedicineRepository _repository;

        public AdminController(IAdminMedicineRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Dashboard()
        {
            var stats = await _repository.GetDashboardStatsAsync();
            ViewBag.LowStockCount = stats.LowStockMedicines;
            ViewBag.ExpiringCount = stats.ExpiringSoonMedicines;

            return View(stats);
        }

        public async Task<IActionResult> MedicineList(string searchTerm = null, string category = null, string status = null)
        {
            List<AdminMedicine> medicines;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                medicines = await _repository.SearchMedicinesAsync(searchTerm, category);
            }
            else
            {
                medicines = await _repository.GetAllMedicinesAsync();
            }

            if (!string.IsNullOrEmpty(status))
            {
                List<AdminMedicine> filteredMedicines = new List<AdminMedicine>();

                foreach (var medicine in medicines)
                {
                    if (status.ToLower() == "low" && medicine.Quantity <= medicine.MinStockLevel && medicine.Quantity > 0)
                    {
                        filteredMedicines.Add(medicine);
                    }
                    else if (status.ToLower() == "expiring" && medicine.ExpiryDate <= System.DateTime.Now.AddDays(30) && medicine.ExpiryDate > System.DateTime.Now)
                    {
                        filteredMedicines.Add(medicine);
                    }
                    else if (status.ToLower() == "expired" && medicine.ExpiryDate <= System.DateTime.Now)
                    {
                        filteredMedicines.Add(medicine);
                    }
                }

                medicines = filteredMedicines;
            }

            // Get unique categories
            List<string> categories = new List<string>();
            foreach (var medicine in medicines)
            {
                if (!categories.Contains(medicine.Category))
                {
                    categories.Add(medicine.Category);
                }
            }

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.SearchTerm = searchTerm;

            var stats = await _repository.GetDashboardStatsAsync();
            ViewBag.LowStockCount = stats.LowStockMedicines;
            ViewBag.ExpiringCount = stats.ExpiringSoonMedicines;

            return View(medicines);
        }

        [HttpGet]
        public async Task<IActionResult> AddMedicine()
        {
            var stats = await _repository.GetDashboardStatsAsync();
            ViewBag.LowStockCount = stats.LowStockMedicines;
            ViewBag.ExpiringCount = stats.ExpiringSoonMedicines;

            var model = new AdminMedicine
            {
                Quantity = 10,
                MinStockLevel = 10,
                Price = 5.99m,
                ExpiryDate = System.DateTime.Now.AddYears(1),
                BatchNumber = $"BATCH-{System.DateTime.Now:yyyyMMdd}",
                Status = "Active"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddMedicine(AdminMedicine medicine, IFormFile ImageFile)
        {
            // Simple validation
            bool isValid = true;

            if (string.IsNullOrEmpty(medicine.Name))
            {
                ModelState.AddModelError("Name", "Medicine Name is required.");
                isValid = false;
            }

            if (string.IsNullOrEmpty(medicine.Category))
            {
                ModelState.AddModelError("Category", "Category is required.");
                isValid = false;
            }

            if (string.IsNullOrEmpty(medicine.Supplier))
            {
                ModelState.AddModelError("Supplier", "Supplier is required.");
                isValid = false;
            }

            if (medicine.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be greater than 0.");
                isValid = false;
            }

            if (medicine.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than 0.");
                isValid = false;
            }

            if (isValid)
            {
                // Handle ImageUrl (either from form or generate placeholder)
                if (string.IsNullOrEmpty(medicine.ImageUrl))
                {
                    medicine.ImageUrl = $"https://via.placeholder.com/250x150/007bff/ffffff?text={System.Net.WebUtility.UrlEncode(medicine.Name)}";
                }

                // Set default values
                medicine.IsActive = true;
                medicine.StockStatus = "In Stock";

                // Call repository asynchronously
                int medicineId = await _repository.AddMedicineAsync(medicine);

                if (medicineId > 0)
                {
                    TempData["SuccessMessage"] = $"Medicine '{medicine.Name}' added successfully!";
                    return RedirectToAction("MedicineList");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add medicine. Please try again.";
                }
            }

            // If validation fails
            var stats = await _repository.GetDashboardStatsAsync();
            ViewBag.LowStockCount = stats.LowStockMedicines;
            ViewBag.ExpiringCount = stats.ExpiringSoonMedicines;

            return View(medicine);
        }

        [HttpGet]
        public async Task<IActionResult> EditMedicine(int id)
        {
            var medicine = await _repository.GetMedicineByIdAsync(id);

            if (medicine == null)
            {
                TempData["ErrorMessage"] = "Medicine not found!";
                return RedirectToAction("MedicineList");
            }

            var stats = await _repository.GetDashboardStatsAsync();
            ViewBag.LowStockCount = stats.LowStockMedicines;
            ViewBag.ExpiringCount = stats.ExpiringSoonMedicines;

            return View(medicine);
        }

        [HttpPost]
        public async Task<IActionResult> EditMedicine(AdminMedicine medicine)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing medicine first
                    var existingMedicine = await _repository.GetMedicineByIdAsync(medicine.MedicineId);

                    // Preserve the ImageUrl if not provided in form
                    if (string.IsNullOrEmpty(medicine.ImageUrl) && existingMedicine != null)
                    {
                        medicine.ImageUrl = existingMedicine.ImageUrl;
                    }

                    // Set a default image if still null
                    if (string.IsNullOrEmpty(medicine.ImageUrl))
                    {
                        medicine.ImageUrl = "/images/default-medicine.jpg";
                    }

                    bool success = await _repository.UpdateMedicineAsync(medicine);

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Medicine updated successfully!";
                        return RedirectToAction("MedicineList");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update medicine.";
                    }
                }
                catch (System.Exception ex)
                {
                    ModelState.AddModelError("", "Error updating medicine: " + ex.Message);
                }
            }

            // If validation fails or update fails
            return View(medicine);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            bool success = await _repository.DeleteMedicineAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Medicine deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete medicine.";
            }

            return RedirectToAction("MedicineList");
        }

        public async Task<IActionResult> LowStock()
        {
            var medicines = await _repository.GetLowStockMedicinesAsync();

            var stats = await _repository.GetDashboardStatsAsync();
            ViewBag.LowStockCount = stats.LowStockMedicines;
            ViewBag.ExpiringCount = stats.ExpiringSoonMedicines;
            ViewBag.SafeStockCount = stats.TotalMedicines - stats.LowStockMedicines;

            return View(medicines);
        }

        public async Task<IActionResult> ExpirySoon()
        {
            var medicines = await _repository.GetExpiringMedicinesAsync();

            var stats = await _repository.GetDashboardStatsAsync();
            var allMedicines = await _repository.GetAllMedicinesAsync();

            // Count expired medicines
            int expiredCount = 0;
            foreach (var medicine in allMedicines)
            {
                if (medicine.ExpiryDate <= System.DateTime.Now)
                {
                    expiredCount++;
                }
            }

            // Count safe medicines (expiry > 90 days)
            int safeCount = 0;
            foreach (var medicine in allMedicines)
            {
                if (medicine.ExpiryDate > System.DateTime.Now.AddDays(90))
                {
                    safeCount++;
                }
            }

            ViewBag.LowStockCount = stats.LowStockMedicines;
            ViewBag.ExpiringCount = stats.ExpiringSoonMedicines;
            ViewBag.ExpiredCount = expiredCount;
            ViewBag.SafeCount = safeCount;

            return View(medicines);
        }

        public async Task<IActionResult> MedicineDetail(int id)
        {
            var medicine = await _repository.GetMedicineByIdAsync(id);

            if (medicine == null)
            {
                TempData["ErrorMessage"] = "Medicine not found!";
                return RedirectToAction("MedicineList");
            }

            var stats = await _repository.GetDashboardStatsAsync();
            ViewBag.LowStockCount = stats.LowStockMedicines;
            ViewBag.ExpiringCount = stats.ExpiringSoonMedicines;

            return View(medicine);
        }
    }
}
