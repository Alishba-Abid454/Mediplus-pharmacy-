using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;

namespace _10PercentWebProject.Repositories
{
    public class AdminMedicineRepository : IAdminMedicineRepository
    {
        private readonly string _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=MedicineDB;Trusted_Connection=True;";

        public async Task<List<AdminMedicine>> GetAllMedicinesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        ISNULL(am.Quantity, 0) as Quantity,
                        ISNULL(am.ExpiryDate, DATEADD(year, 2, GETDATE())) as ExpiryDate,
                        ISNULL(am.Supplier, 'Not Specified') as Supplier,
                        ISNULL(am.BatchNumber, 'N/A') as BatchNumber,
                        ISNULL(am.MinStockLevel, 10) as MinStockLevel,
                        ISNULL(am.BrandName, m.Name) as BrandName,
                        ISNULL(am.Status, 'Active') as Status
                    FROM Medicines m
                    LEFT JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE m.IsActive = 1
                    ORDER BY m.Name";

                var result = await connection.QueryAsync<AdminMedicine>(sql);
                return result.ToList();
            }
        }

        public async Task<AdminMedicine> GetMedicineByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        ISNULL(am.Quantity, 0) as Quantity,
                        ISNULL(am.ExpiryDate, DATEADD(year, 2, GETDATE())) as ExpiryDate,
                        ISNULL(am.Supplier, 'Not Specified') as Supplier,
                        ISNULL(am.BatchNumber, 'N/A') as BatchNumber,
                        ISNULL(am.MinStockLevel, 10) as MinStockLevel,
                        ISNULL(am.BrandName, m.Name) as BrandName,
                        ISNULL(am.Status, 'Active') as Status
                    FROM Medicines m
                    LEFT JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE m.MedicineId = @Id AND m.IsActive = 1";

                return await connection.QueryFirstOrDefaultAsync<AdminMedicine>(
                    sql, new { Id = id });
            }
        }

        public async Task<int> AddMedicineAsync(AdminMedicine medicine)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Set default values
                medicine.Description ??= "No description provided";
                medicine.ImageUrl ??= "/images/default-medicine.jpg";
                medicine.BrandName ??= medicine.Name;
                medicine.BatchNumber ??= $"BATCH-{DateTime.Now:yyyyMMdd}";
                medicine.Supplier ??= "General Supplier";
                medicine.IsFeatured = false;
                medicine.IsOnSale = false;
                medicine.IsActive = true;
                medicine.StockStatus = "In Stock";
                medicine.BadgeType = "New";

                if (medicine.MinStockLevel <= 0)
                    medicine.MinStockLevel = 10;

                // Insert into Medicines
                string sql = @"
                    INSERT INTO Medicines 
                    (Name, Description, Category, Price, ImageUrl, StockStatus, 
                     IsFeatured, IsOnSale, BadgeType, IsActive)
                    VALUES 
                    (@Name, @Description, @Category, @Price, @ImageUrl, @StockStatus,
                     @IsFeatured, @IsOnSale, @BadgeType, @IsActive);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                int medicineId = await connection.ExecuteScalarAsync<int>(sql, medicine);

                // Insert into AdminMedicines
                string adminSql = @"
                    INSERT INTO AdminMedicines 
                    (MedicineId, Quantity, ExpiryDate, Supplier, BatchNumber, 
                     MinStockLevel, BrandName, Status)
                    VALUES 
                    (@MedicineId, @Quantity, @ExpiryDate, @Supplier, @BatchNumber, 
                     @MinStockLevel, @BrandName, 'Active')";

                await connection.ExecuteAsync(adminSql, new
                {
                    MedicineId = medicineId,
                    medicine.Quantity,
                    medicine.ExpiryDate,
                    medicine.Supplier,
                    medicine.BatchNumber,
                    medicine.MinStockLevel,
                    medicine.BrandName
                });

                return medicineId;
            }
        }

        public async Task<bool> UpdateMedicineAsync(AdminMedicine medicine)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Update Medicines
                string updateSql = @"
                    UPDATE Medicines 
                    SET Name = @Name,
                        Description = @Description,
                        Category = @Category,
                        Price = @Price,
                        ImageUrl = @ImageUrl
                    WHERE MedicineId = @MedicineId";

                await connection.ExecuteAsync(updateSql, medicine);

                // Check if AdminMedicines exists
                int exists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM AdminMedicines WHERE MedicineId = @MedicineId",
                    new { medicine.MedicineId });

                if (exists > 0)
                {
                    string adminSql = @"
                        UPDATE AdminMedicines 
                        SET Quantity = @Quantity,
                            ExpiryDate = @ExpiryDate,
                            Supplier = @Supplier,
                            BatchNumber = @BatchNumber,
                            MinStockLevel = @MinStockLevel,
                            BrandName = @BrandName
                        WHERE MedicineId = @MedicineId";

                    var rowsAffected = await connection.ExecuteAsync(adminSql, medicine);
                    return rowsAffected > 0;
                }
                else
                {
                    string insertSql = @"
                        INSERT INTO AdminMedicines 
                        (MedicineId, Quantity, ExpiryDate, Supplier, BatchNumber, 
                         MinStockLevel, BrandName, Status)
                        VALUES 
                        (@MedicineId, @Quantity, @ExpiryDate, @Supplier, @BatchNumber, 
                         @MinStockLevel, @BrandName, 'Active')";

                    var rowsAffected = await connection.ExecuteAsync(insertSql, medicine);
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<bool> DeleteMedicineAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Medicines SET IsActive = 0 WHERE MedicineId = @Id";
                var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                return rowsAffected > 0;
            }
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var stats = new DashboardStats();

                // Total Medicines
                stats.TotalMedicines = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Medicines WHERE IsActive = 1");

                // Low Stock Medicines
                try
                {
                    stats.LowStockMedicines = await connection.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM Medicines m
                        WHERE m.IsActive = 1 
                        AND EXISTS (
                            SELECT 1 FROM AdminMedicines am 
                            WHERE am.MedicineId = m.MedicineId 
                            AND am.Quantity <= am.MinStockLevel
                        )");
                }
                catch
                {
                    stats.LowStockMedicines = 0;
                }

                // Expiring Soon
                try
                {
                    stats.ExpiringSoonMedicines = await connection.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM Medicines m
                        WHERE m.IsActive = 1 
                        AND EXISTS (
                            SELECT 1 FROM AdminMedicines am 
                            WHERE am.MedicineId = m.MedicineId 
                            AND am.ExpiryDate <= DATEADD(day, 30, GETDATE())
                            AND am.ExpiryDate > GETDATE()
                        )");
                }
                catch
                {
                    stats.ExpiringSoonMedicines = 0;
                }

                // Total Categories
                stats.TotalCategories = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(DISTINCT Category) FROM Medicines WHERE IsActive = 1");

                return stats;
            }
        }

        public async Task<List<AdminMedicine>> GetExpiringMedicinesAsync(int daysThreshold = 30)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        am.Quantity, am.ExpiryDate, am.Supplier,
                        am.BatchNumber, am.MinStockLevel, am.BrandName, am.Status,
                        DATEDIFF(day, GETDATE(), am.ExpiryDate) as DaysUntilExpiry
                    FROM Medicines m
                    INNER JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE am.ExpiryDate <= DATEADD(day, @DaysThreshold, GETDATE())
                    AND am.ExpiryDate > GETDATE()
                    AND m.IsActive = 1
                    ORDER BY am.ExpiryDate";

                var result = await connection.QueryAsync<AdminMedicine>(
                    sql, new { DaysThreshold = daysThreshold });
                return result.ToList();
            }
        }

        public async Task<List<AdminMedicine>> GetLowStockMedicinesAsync(int threshold = 10)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        am.Quantity, am.ExpiryDate, am.Supplier,
                        am.BatchNumber, am.MinStockLevel, am.BrandName, am.Status
                    FROM Medicines m
                    INNER JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE am.Quantity <= @Threshold
                    AND m.IsActive = 1
                    ORDER BY am.Quantity";

                var result = await connection.QueryAsync<AdminMedicine>(
                    sql, new { Threshold = threshold });
                return result.ToList();
            }
        }

        public async Task<List<AdminMedicine>> SearchMedicinesAsync(string searchTerm, string category = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        ISNULL(am.Quantity, 0) as Quantity,
                        ISNULL(am.ExpiryDate, DATEADD(year, 2, GETDATE())) as ExpiryDate,
                        ISNULL(am.Supplier, 'Not Specified') as Supplier,
                        ISNULL(am.BatchNumber, 'N/A') as BatchNumber,
                        ISNULL(am.MinStockLevel, 10) as MinStockLevel,
                        ISNULL(am.BrandName, m.Name) as BrandName,
                        ISNULL(am.Status, 'Active') as Status
                    FROM Medicines m
                    LEFT JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE m.IsActive = 1
                    AND (m.Name LIKE @SearchTerm 
                         OR m.Description LIKE @SearchTerm 
                         OR m.Category LIKE @SearchTerm
                         OR ISNULL(am.BrandName, '') LIKE @SearchTerm)";

                if (!string.IsNullOrEmpty(category))
                {
                    sql += " AND m.Category = @Category";
                }

                sql += " ORDER BY m.Name";

                var result = await connection.QueryAsync<AdminMedicine>(sql, new
                {
                    SearchTerm = $"%{searchTerm}%",
                    Category = category
                });
                return result.ToList();
            }
        }

        public async Task<bool> UpdateStockAsync(int medicineId, int newQuantity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    // Update AdminMedicines
                    string updateAdminSql = @"
                        UPDATE AdminMedicines 
                        SET Quantity = @Quantity,
                            Status = CASE 
                                WHEN @Quantity <= 0 THEN 'Out of Stock'
                                WHEN @Quantity <= MinStockLevel THEN 'Low Stock'
                                ELSE 'Active'
                            END,
                            LastUpdated = GETDATE()
                        WHERE MedicineId = @MedicineId";

                    int adminRows = await connection.ExecuteAsync(updateAdminSql, new
                    {
                        Quantity = newQuantity,
                        MedicineId = medicineId
                    }, transaction);

                    // Update Medicines
                    string updateMedicineSql = @"
                        UPDATE Medicines 
                        SET StockStatus = CASE 
                            WHEN @Quantity <= 0 THEN 'Out of Stock'
                            WHEN @Quantity <= (
                                SELECT MinStockLevel FROM AdminMedicines 
                                WHERE MedicineId = @MedicineId
                            ) THEN 'Low Stock'
                            ELSE 'In Stock'
                        END,
                        BadgeType = CASE 
                            WHEN @Quantity <= (
                                SELECT MinStockLevel FROM AdminMedicines 
                                WHERE MedicineId = @MedicineId
                            ) THEN 'Low Stock'
                            WHEN IsFeatured = 1 THEN 'Featured'
                            ELSE ''
                        END
                        WHERE MedicineId = @MedicineId";

                    int medicineRows = await connection.ExecuteAsync(updateMedicineSql, new
                    {
                        Quantity = newQuantity,
                        MedicineId = medicineId
                    }, transaction);

                    await transaction.CommitAsync();
                    return adminRows > 0 || medicineRows > 0;
                }
            }
        }

        public async Task<bool> UpdateExpiryStatusAsync(int medicineId, DateTime newExpiryDate)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    string updateAdminSql = @"
                        UPDATE AdminMedicines 
                        SET ExpiryDate = @ExpiryDate,
                            Status = CASE 
                                WHEN @ExpiryDate <= GETDATE() THEN 'Expired'
                                WHEN DATEDIFF(day, GETDATE(), @ExpiryDate) <= 30 THEN 'Expiring Soon'
                                ELSE 'Active'
                            END,
                            LastUpdated = GETDATE()
                        WHERE MedicineId = @MedicineId";

                    int adminRows = await connection.ExecuteAsync(updateAdminSql, new
                    {
                        ExpiryDate = newExpiryDate,
                        MedicineId = medicineId
                    }, transaction);

                    await transaction.CommitAsync();
                    return adminRows > 0;
                }
            }
        }
    }
}



/*using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using Dapper;
using Microsoft.Data.SqlClient;

namespace _10PercentWebProject.Repositories
{
    public class AdminMedicineRepository : IAdminMedicineRepository
    {
        private readonly string _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=MedicineDB;Trusted_Connection=True;";

        public List<AdminMedicine> GetAllMedicines()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        ISNULL(am.Quantity, 0) as Quantity,
                        ISNULL(am.ExpiryDate, DATEADD(year, 2, GETDATE())) as ExpiryDate,
                        ISNULL(am.Supplier, 'Not Specified') as Supplier,
                        ISNULL(am.BatchNumber, 'N/A') as BatchNumber,
                        ISNULL(am.MinStockLevel, 10) as MinStockLevel,
                        ISNULL(am.BrandName, m.Name) as BrandName,
                        ISNULL(am.Status, 'Active') as Status
                    FROM Medicines m
                    LEFT JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE m.IsActive = 1
                    ORDER BY m.Name";

                return connection.Query<AdminMedicine>(sql).ToList();
            }
        }

        public AdminMedicine GetMedicineById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        ISNULL(am.Quantity, 0) as Quantity,
                        ISNULL(am.ExpiryDate, DATEADD(year, 2, GETDATE())) as ExpiryDate,
                        ISNULL(am.Supplier, 'Not Specified') as Supplier,
                        ISNULL(am.BatchNumber, 'N/A') as BatchNumber,
                        ISNULL(am.MinStockLevel, 10) as MinStockLevel,
                        ISNULL(am.BrandName, m.Name) as BrandName,
                        ISNULL(am.Status, 'Active') as Status
                    FROM Medicines m
                    LEFT JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE m.MedicineId = @Id AND m.IsActive = 1";

                return connection.QueryFirstOrDefault<AdminMedicine>(sql, new { Id = id });
            }
        }

        public int AddMedicine(AdminMedicine medicine)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Set default values for ALL required columns
                medicine.Description ??= "No description provided";
                medicine.ImageUrl ??= "/images/default-medicine.jpg";
                medicine.BrandName ??= medicine.Name;
                medicine.BatchNumber ??= $"BATCH-{DateTime.Now:yyyyMMdd}";
                medicine.Supplier ??= "General Supplier";

                // Set defaults for Medicines table columns
                medicine.IsFeatured = false; // Default to false
                medicine.IsOnSale = false;
                medicine.IsActive = true;
                medicine.StockStatus = "In Stock";
                medicine.BadgeType = "New";

                if (medicine.MinStockLevel <= 0)
                    medicine.MinStockLevel = 10;

                string sql = @"
            INSERT INTO Medicines 
            (Name, Description, Category, Price, ImageUrl, StockStatus, 
             IsFeatured, IsOnSale, BadgeType, IsActive)
            VALUES 
            (@Name, @Description, @Category, @Price, @ImageUrl, @StockStatus,
             @IsFeatured, @IsOnSale, @BadgeType, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int)";

                int medicineId = connection.ExecuteScalar<int>(sql, medicine);

                // Insert into AdminMedicines
                string adminSql = @"
            INSERT INTO AdminMedicines 
            (MedicineId, Quantity, ExpiryDate, Supplier, BatchNumber, MinStockLevel, BrandName, Status)
            VALUES 
            (@MedicineId, @Quantity, @ExpiryDate, @Supplier, @BatchNumber, @MinStockLevel, @BrandName, 'Active')";

                connection.Execute(adminSql, new
                {
                    MedicineId = medicineId,
                    medicine.Quantity,
                    medicine.ExpiryDate,
                    medicine.Supplier,
                    medicine.BatchNumber,
                    medicine.MinStockLevel,
                    medicine.BrandName
                });

                return medicineId;
            }
        }
        public bool UpdateMedicine(AdminMedicine medicine)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Update Medicines
                string updateSql = @"
        UPDATE Medicines 
        SET Name = @Name,
            Description = @Description,
            Category = @Category,
            Price = @Price,
            ImageUrl = @ImageUrl
        WHERE MedicineId = @MedicineId";

                connection.Execute(updateSql, medicine);

                // Check if AdminMedicines exists
                int exists = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM AdminMedicines WHERE MedicineId = @MedicineId",
                    new { medicine.MedicineId });

                if (exists > 0)
                {
                    string adminSql = @"
            UPDATE AdminMedicines 
            SET Quantity = @Quantity,
                ExpiryDate = @ExpiryDate,
                Supplier = @Supplier,
                BatchNumber = @BatchNumber,
                MinStockLevel = @MinStockLevel,
                BrandName = @BrandName
            WHERE MedicineId = @MedicineId";

                    return connection.Execute(adminSql, medicine) > 0;
                }
                else
                {
                    string insertSql = @"
            INSERT INTO AdminMedicines 
            (MedicineId, Quantity, ExpiryDate, Supplier, BatchNumber, MinStockLevel, BrandName, Status)
            VALUES 
            (@MedicineId, @Quantity, @ExpiryDate, @Supplier, @BatchNumber, @MinStockLevel, @BrandName, 'Active')";

                    return connection.Execute(insertSql, medicine) > 0;
                }
            }
        }


        *//*  public bool UpdateMedicine(AdminMedicine medicine)
          {
              using (var connection = new SqlConnection(_connectionString))
              {
                  // Update Medicines table
                  string updateSql = @"
              UPDATE Medicines 
              SET Name = @Name,
                  Description = @Description,
                  Category = @Category,
                  Price = @Price,
                  ImageUrl = @ImageUrl
              WHERE MedicineId = @MedicineId";

                  connection.Execute(updateSql, medicine);

                  // Update AdminMedicines table
                  string adminSql = @"
              UPDATE AdminMedicines 
              SET Quantity = @Quantity,
                  ExpiryDate = @ExpiryDate,
                  Supplier = @Supplier,
                  BatchNumber = @BatchNumber,
                  MinStockLevel = @MinStockLevel,
                  BrandName = @BrandName
              WHERE MedicineId = @MedicineId";

                  int rowsAffected = connection.Execute(adminSql, medicine);
                  return rowsAffected > 0;
              }
          }*//*

        public bool DeleteMedicine(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE Medicines SET IsActive = 0 WHERE MedicineId = @Id";
                return connection.Execute(sql, new { Id = id }) > 0;
            }
        }

        public DashboardStats GetDashboardStats()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var stats = new DashboardStats();

                // Total Medicines
                stats.TotalMedicines = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM Medicines WHERE IsActive = 1");

                // Low Stock Medicines - FIXED: Check if AdminMedicines exists
                try
                {
                    stats.LowStockMedicines = connection.ExecuteScalar<int>(@"
                SELECT COUNT(*) FROM Medicines m
                WHERE m.IsActive = 1 
                AND EXISTS (
                    SELECT 1 FROM AdminMedicines am 
                    WHERE am.MedicineId = m.MedicineId 
                    AND am.Quantity <= am.MinStockLevel
                )");
                }
                catch
                {
                    stats.LowStockMedicines = 0; // Set to 0 if query fails
                }

                // Expiring Soon (within 30 days)
                try
                {
                    stats.ExpiringSoonMedicines = connection.ExecuteScalar<int>(@"
                SELECT COUNT(*) FROM Medicines m
                WHERE m.IsActive = 1 
                AND EXISTS (
                    SELECT 1 FROM AdminMedicines am 
                    WHERE am.MedicineId = m.MedicineId 
                    AND am.ExpiryDate <= DATEADD(day, 30, GETDATE())
                    AND am.ExpiryDate > GETDATE()
                )");
                }
                catch
                {
                    stats.ExpiringSoonMedicines = 0; // Set to 0 if query fails
                }

                // Total Categories
                stats.TotalCategories = connection.ExecuteScalar<int>(
                    "SELECT COUNT(DISTINCT Category) FROM Medicines WHERE IsActive = 1");

                return stats;
            }
        }
        *//*        public DashboardStats GetDashboardStats()
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        var stats = new DashboardStats();

                        // Total Medicines
                        stats.TotalMedicines = connection.ExecuteScalar<int>(
                            "SELECT COUNT(*) FROM Medicines WHERE IsActive = 1");

                        // Low Stock Medicines
                        stats.LowStockMedicines = connection.ExecuteScalar<int>(@"
                            SELECT COUNT(*) FROM AdminMedicines am
                            INNER JOIN Medicines m ON am.MedicineId = m.MedicineId
                            WHERE am.Quantity <= am.MinStockLevel 
                            AND m.IsActive = 1");

                        // Expiring Soon (within 30 days)
                        stats.ExpiringSoonMedicines = connection.ExecuteScalar<int>(@"
                            SELECT COUNT(*) FROM AdminMedicines am
                            INNER JOIN Medicines m ON am.MedicineId = m.MedicineId
                            WHERE am.ExpiryDate <= DATEADD(day, 30, GETDATE())
                            AND am.ExpiryDate > GETDATE()
                            AND m.IsActive = 1");

                        // Total Categories
                        stats.TotalCategories = connection.ExecuteScalar<int>(
                            "SELECT COUNT(DISTINCT Category) FROM Medicines WHERE IsActive = 1");

                        return stats;
                    }
                }*//*

        public List<AdminMedicine> GetExpiringMedicines(int daysThreshold = 30)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        am.Quantity, am.ExpiryDate, am.Supplier,
                        am.BatchNumber, am.MinStockLevel, am.BrandName, am.Status,
                        DATEDIFF(day, GETDATE(), am.ExpiryDate) as DaysUntilExpiry
                    FROM Medicines m
                    INNER JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE am.ExpiryDate <= DATEADD(day, @DaysThreshold, GETDATE())
                    AND am.ExpiryDate > GETDATE()
                    AND m.IsActive = 1
                    ORDER BY am.ExpiryDate";

                return connection.Query<AdminMedicine>(sql, new { DaysThreshold = daysThreshold }).ToList();
            }
        }

        public List<AdminMedicine> GetLowStockMedicines(int threshold = 10)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        am.Quantity, am.ExpiryDate, am.Supplier,
                        am.BatchNumber, am.MinStockLevel, am.BrandName, am.Status
                    FROM Medicines m
                    INNER JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE am.Quantity <= @Threshold
                    AND m.IsActive = 1
                    ORDER BY am.Quantity";

                return connection.Query<AdminMedicine>(sql, new { Threshold = threshold }).ToList();
            }
        }

        public List<AdminMedicine> SearchMedicines(string searchTerm, string category = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT 
                        m.MedicineId, m.Name, m.Description, m.Category, 
                        m.Price, m.ImageUrl, m.StockStatus, m.IsFeatured,
                        m.IsOnSale, m.BadgeType, m.IsActive,
                        ISNULL(am.Quantity, 0) as Quantity,
                        ISNULL(am.ExpiryDate, DATEADD(year, 2, GETDATE())) as ExpiryDate,
                        ISNULL(am.Supplier, 'Not Specified') as Supplier,
                        ISNULL(am.BatchNumber, 'N/A') as BatchNumber,
                        ISNULL(am.MinStockLevel, 10) as MinStockLevel,
                        ISNULL(am.BrandName, m.Name) as BrandName,
                        ISNULL(am.Status, 'Active') as Status
                    FROM Medicines m
                    LEFT JOIN AdminMedicines am ON m.MedicineId = am.MedicineId
                    WHERE m.IsActive = 1
                    AND (m.Name LIKE @SearchTerm 
                         OR m.Description LIKE @SearchTerm 
                         OR m.Category LIKE @SearchTerm
                         OR ISNULL(am.BrandName, '') LIKE @SearchTerm)";

                if (!string.IsNullOrEmpty(category))
                {
                    sql += " AND m.Category = @Category";
                }

                sql += " ORDER BY m.Name";

                return connection.Query<AdminMedicine>(sql, new
                {
                    SearchTerm = $"%{searchTerm}%",
                    Category = category
                }).ToList();
            }
        }

        public bool UpdateStock(int medicineId, int newQuantity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    // Update AdminMedicines table
                    string updateAdminSql = @"
                        UPDATE AdminMedicines 
                        SET Quantity = @Quantity,
                            Status = CASE 
                                WHEN @Quantity <= 0 THEN 'Out of Stock'
                                WHEN @Quantity <= MinStockLevel THEN 'Low Stock'
                                ELSE 'Active'
                            END,
                            LastUpdated = GETDATE()
                        WHERE MedicineId = @MedicineId";

                    int adminRows = connection.Execute(updateAdminSql, new
                    {
                        Quantity = newQuantity,
                        MedicineId = medicineId
                    }, transaction);

                    // Update Medicines table StockStatus
                    string updateMedicineSql = @"
                        UPDATE Medicines 
                        SET StockStatus = CASE 
                            WHEN @Quantity <= 0 THEN 'Out of Stock'
                            WHEN @Quantity <= (
                                SELECT MinStockLevel FROM AdminMedicines 
                                WHERE MedicineId = @MedicineId
                            ) THEN 'Low Stock'
                            ELSE 'In Stock'
                        END,
                        BadgeType = CASE 
                            WHEN @Quantity <= (
                                SELECT MinStockLevel FROM AdminMedicines 
                                WHERE MedicineId = @MedicineId
                            ) THEN 'Low Stock'
                            WHEN IsFeatured = 1 THEN 'Featured'
                            ELSE ''
                        END
                        WHERE MedicineId = @MedicineId";

                    int medicineRows = connection.Execute(updateMedicineSql, new
                    {
                        Quantity = newQuantity,
                        MedicineId = medicineId
                    }, transaction);

                    transaction.Commit();
                    return adminRows > 0 || medicineRows > 0;
                }
            }
        }

        public bool UpdateExpiryStatus(int medicineId, DateTime newExpiryDate)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    // Update AdminMedicines table
                    string updateAdminSql = @"
                        UPDATE AdminMedicines 
                        SET ExpiryDate = @ExpiryDate,
                            Status = CASE 
                                WHEN @ExpiryDate <= GETDATE() THEN 'Expired'
                                WHEN DATEDIFF(day, GETDATE(), @ExpiryDate) <= 30 THEN 'Expiring Soon'
                                ELSE 'Active'
                            END,
                            LastUpdated = GETDATE()
                        WHERE MedicineId = @MedicineId";

                    int adminRows = connection.Execute(updateAdminSql, new
                    {
                        ExpiryDate = newExpiryDate,
                        MedicineId = medicineId
                    }, transaction);

                    transaction.Commit();
                    return adminRows > 0;
                }
            }
        }
    }
}


*/