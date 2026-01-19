using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using _10PercentWebProject.Controllers;
using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTestingForProject.UnitTest
{
    public class AdminControllerTest
    {
        [Fact]
        public async Task Dashboard_Returns_ViewResult()
        {
            // Arrange
            var mockRepo = new Mock<IAdminMedicineRepository>();

            mockRepo.Setup(r => r.GetDashboardStatsAsync())
                .ReturnsAsync(new DashboardStats
                {
                    LowStockMedicines = 2,
                    ExpiringSoonMedicines = 1,
                    TotalMedicines = 10
                });

            var controller = new AdminController(mockRepo.Object);

            // Act
            var result = await controller.Dashboard();

            // Assert
            Assert.IsType<ViewResult>(result);
        }
        [Fact]
        public async Task MedicineList_Returns_ViewResult()
        {
            // Arrange
            var mockRepo = new Mock<IAdminMedicineRepository>();

            mockRepo.Setup(r => r.GetAllMedicinesAsync())
                .ReturnsAsync(new List<AdminMedicine>());

            mockRepo.Setup(r => r.GetDashboardStatsAsync())
                .ReturnsAsync(new DashboardStats());

            var controller = new AdminController(mockRepo.Object);

            // Act
            var result = await controller.MedicineList();

            // Assert
            Assert.IsType<ViewResult>(result);
        }
    }

}
