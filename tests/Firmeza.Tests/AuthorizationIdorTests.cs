using System.Security.Claims;
using System.Text;
using AutoMapper;
using Firmeza.Api.Controllers;
using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Firmeza.Tests;

public class AuthorizationIdorTests
{
    private readonly Mock<ISaleService> _mockSaleService;
    private readonly Mock<ICustomerRepository> _mockCustomerRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPdfService> _mockPdfService;

    public AuthorizationIdorTests()
    {
        _mockSaleService = new Mock<ISaleService>();
        _mockCustomerRepo = new Mock<ICustomerRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockPdfService = new Mock<IPdfService>();
    }

    private static ControllerContext CreateContextForUser(string userId, string email, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetSaleById_WhenCustomerRequestsSaleOfAnotherCustomer_Returns404NotFound()
    {
        // Arrange
        var customerAId = Guid.NewGuid();
        var customerBId = Guid.NewGuid();
        var userBId = "user-b-guid";
        var saleId = Guid.NewGuid();

        var saleOfA = new SaleDto { Id = saleId, CustomerId = customerAId };
        var customerB = new Customer { Id = customerBId, UserId = userBId, Email = "clienteB@test.com" };

        _mockSaleService.Setup(s => s.GetSaleByIdAsync(saleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saleOfA);

        _mockCustomerRepo.Setup(r => r.GetByUserIdAsync(userBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerB);

        var controller = new SalesController(_mockSaleService.Object, _mockCustomerRepo.Object)
        {
            ControllerContext = CreateContextForUser(userBId, "clienteB@test.com", "Cliente")
        };

        // Act
        var result = await controller.GetById(saleId, CancellationToken.None);

        // Assert: 404 esperado para mitigar IDOR y evitar enumeración de recursos
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetReceipt_WhenCustomerRequestsReceiptOfAnotherCustomer_Returns404NotFound()
    {
        // Arrange
        var customerAId = Guid.NewGuid();
        var customerBId = Guid.NewGuid();
        var userBId = "user-b-guid";
        var saleId = Guid.NewGuid();

        var saleOfA = new SaleDto { Id = saleId, CustomerId = customerAId };
        var customerB = new Customer { Id = customerBId, UserId = userBId, Email = "clienteB@test.com" };

        _mockSaleService.Setup(s => s.GetSaleByIdAsync(saleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saleOfA);

        _mockCustomerRepo.Setup(r => r.GetByUserIdAsync(userBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerB);

        var controller = new SalesController(_mockSaleService.Object, _mockCustomerRepo.Object)
        {
            ControllerContext = CreateContextForUser(userBId, "clienteB@test.com", "Cliente")
        };

        // Act
        var result = await controller.GetReceipt(saleId, CancellationToken.None);

        // Assert: 404 esperado
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateSale_WhenCustomerTriesToSpoofCustomerIdOfAnother_OverridesWithOwnCustomerId()
    {
        // Arrange
        var customerAId = Guid.NewGuid();
        var customerBId = Guid.NewGuid();
        var userBId = "user-b-guid";

        var customerB = new Customer { Id = customerBId, UserId = userBId, Email = "clienteB@test.com" };

        var createDto = new CreateSaleDto
        {
            CustomerId = customerAId, // Cliente B intenta suplantar el CustomerId de A
            Items = [new CartItemDto { ProductId = Guid.NewGuid(), Quantity = 1 }]
        };

        _mockCustomerRepo.Setup(r => r.GetByUserIdAsync(userBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerB);

        _mockSaleService.Setup(s => s.CreateSaleAsync(It.IsAny<CreateSaleDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateSaleDto dto, CancellationToken _) => new SaleDto { Id = Guid.NewGuid(), CustomerId = dto.CustomerId });

        var controller = new SalesController(_mockSaleService.Object, _mockCustomerRepo.Object)
        {
            ControllerContext = CreateContextForUser(userBId, "clienteB@test.com", "Cliente")
        };

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert: El controller debió forzar el CustomerId propio de B
        Assert.Equal(customerBId, createDto.CustomerId);
    }

    [Fact]
    public async Task GetSaleById_WhenAdminRequestsAnySale_Returns200Ok()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var sale = new SaleDto { Id = saleId, CustomerId = Guid.NewGuid() };

        _mockSaleService.Setup(s => s.GetSaleByIdAsync(saleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sale);

        var controller = new SalesController(_mockSaleService.Object, _mockCustomerRepo.Object)
        {
            ControllerContext = CreateContextForUser("admin-id", "admin@firmeza.com", "Administrador")
        };

        // Act
        var result = await controller.GetById(saleId, CancellationToken.None);

        // Assert: Administrador tiene acceso total
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(sale, okResult.Value);
    }

    [Fact]
    public async Task GetCustomerById_WhenCustomerRequestsProfileOfAnother_Returns403Forbid()
    {
        // Arrange
        var customerAId = Guid.NewGuid();
        var customerA = new Customer { Id = customerAId, UserId = "user-a-guid", Email = "clienteA@test.com" };

        _mockCustomerRepo.Setup(r => r.GetByIdAsync(customerAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerA);

        var controller = new CustomersController(_mockCustomerRepo.Object, _mockUow.Object, _mockMapper.Object, _mockPdfService.Object)
        {
            ControllerContext = CreateContextForUser("user-b-guid", "clienteB@test.com", "Cliente")
        };

        // Act
        var result = await controller.GetById(customerAId, CancellationToken.None);

        // Assert: Retorna 403 Prohibido
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task ImportProducts_WhenFileIsTextFileRenamedToXlsx_Returns400BadRequest()
    {
        // Arrange
        var mockExcelService = new Mock<IExcelService>();
        var controller = new ExcelController(mockExcelService.Object);

        // Simular un archivo .txt con contenido de texto plano renombrado a .xlsx
        var plainTextBytes = Encoding.UTF8.GetBytes("Este es un archivo de texto plano no un excel real.");
        using var stream = new MemoryStream(plainTextBytes);

        var formFile = new FormFile(stream, 0, plainTextBytes.Length, "file", "datos_falsos.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        // Act
        var result = await controller.ImportProducts(formFile, CancellationToken.None);

        // Assert: Debe retornar 400 BadRequest debido a que la firma de bytes mágicos no coincide con PK\x03\x04
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }
}
