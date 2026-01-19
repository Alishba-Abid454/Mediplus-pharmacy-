using _10PercentWebProject.Controllers;
using _10PercentWebProject.Hubs;
using _10PercentWebProject.Models;
using _10PercentWebProject.Models.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestingForProject.UnitTest
{
    public class MedicineControllerTest
    {
        private MedicineController GetControllerWithMocks(
            out Mock<IMedicineRepository> mockRepo,
            out Mock<IHubContext<InventoryHub>> mockInventoryHub,
            out Mock<IHubContext<OrderHub>> mockOrderHub)
        {
            mockRepo = new Mock<IMedicineRepository>();
            mockInventoryHub = new Mock<IHubContext<InventoryHub>>();
            mockOrderHub = new Mock<IHubContext<OrderHub>>();

            var controller = new MedicineController(
                mockRepo.Object,
                mockInventoryHub.Object,
                mockOrderHub.Object
            );

            // Set up session
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new MockHttpSession();
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [Fact]
        public async Task HomePage_Returns_ViewResult()
        {
            // Arrange
            var controller = GetControllerWithMocks(
                out var mockRepo, out var mockInventoryHub, out var mockOrderHub);

            mockRepo.Setup(r => r.GetAllMedicinesAsync())
                .ReturnsAsync(new List<Medicine>());

            mockRepo.Setup(r => r.GetFeaturedMedicinesAsync())
                .ReturnsAsync(new List<Medicine>());

            mockRepo.Setup(r => r.GetOnSaleMedicinesAsync())
                .ReturnsAsync(new List<Medicine>());

            mockRepo.Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(new List<string>());

            mockRepo.Setup(r => r.GetCartItemCount()).Returns(0);

            // Act
            var result = await controller.HomePage();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task MedicineDetail_Returns_ViewResult_When_MedicineExists()
        {
            // Arrange
            var controller = GetControllerWithMocks(
                out var mockRepo, out var mockInventoryHub, out var mockOrderHub);

            var medicine = new Medicine { MedicineId = 1, Name = "Paracetamol", Category = "Painkiller" };

            mockRepo.Setup(r => r.GetMedicineByIdAsync(1)).ReturnsAsync(medicine);
            mockRepo.Setup(r => r.GetMedicinesByCategoryAsync("Painkiller")).ReturnsAsync(new List<Medicine>());
            mockRepo.Setup(r => r.GetCartItemCount()).Returns(1);

            // Act
            var result = await controller.MedicineDetail(1);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Search_Returns_ViewResult()
        {
            // Arrange
            var controller = GetControllerWithMocks(
                out var mockRepo, out var mockInventoryHub, out var mockOrderHub);

            mockRepo.Setup(r => r.GetAllMedicinesAsync()).ReturnsAsync(new List<Medicine>());
            mockRepo.Setup(r => r.SearchMedicinesAsync("test")).ReturnsAsync(new List<Medicine>());
            mockRepo.Setup(r => r.GetCartItemCount()).Returns(0);

            // Act
            var result = await controller.Search("test");

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Cart_Returns_ViewResult()
        {
            // Arrange
            var controller = GetControllerWithMocks(
                out var mockRepo, out var mockInventoryHub, out var mockOrderHub);

            mockRepo.Setup(r => r.GetCartItems()).Returns(new List<CartItem>());
            mockRepo.Setup(r => r.GetCartTotal()).Returns(100);
            mockRepo.Setup(r => r.GetCartItemCount()).Returns(1);

            // Act
            var result = controller.Cart();

            // Assert
            Assert.IsType<ViewResult>(result);
        }
    }

    // Simple mock session class
    public class MockHttpSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new();
        public IEnumerable<string> Keys => _sessionStorage.Keys;
        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;

        public void Clear() => _sessionStorage.Clear();
        public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _sessionStorage.Remove(key);
        public void Set(string key, byte[] value) => _sessionStorage[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
    }
}
