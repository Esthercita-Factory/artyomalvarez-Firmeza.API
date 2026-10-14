using AutoMapper;
using Firmeza.Application.Common.Exceptions;
using Firmeza.Application.DTOs.Dashboard;
using Firmeza.Application.DTOs.Rentals;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Persistence;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;

namespace Firmeza.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPdfService _pdfService;
    private readonly IEmailService _emailService;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPdfService pdfService,
        IEmailService emailService)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _pdfService = pdfService;
        _emailService = emailService;
    }

    public async Task<CartCalculationDto> CalculateCartAsync(List<CartItemDto> items, decimal taxRate = 0.19m, CancellationToken cancellationToken = default)
    {
        var calculation = new CartCalculationDto { TaxRate = taxRate };

        foreach (var item in items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken)
                ?? throw new NotFoundException($"El producto con ID {item.ProductId} no fue encontrado.");

            var lineTotal = product.UnitPrice * item.Quantity;
            calculation.Subtotal += lineTotal;

            calculation.Items.Add(new SaleDetailDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSku = product.Sku,
                Quantity = item.Quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = lineTotal
            });
        }

        calculation.TaxAmount = Math.Round(calculation.Subtotal * taxRate, 2);
        calculation.Total = calculation.Subtotal + calculation.TaxAmount;

        return calculation;
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"El cliente con ID {dto.CustomerId} no existe.");

        var cart = await CalculateCartAsync(dto.Items, dto.TaxRate, cancellationToken);

        var sale = new Sale
        {
            CustomerId = customer.Id,
            ExternalReference = dto.ExternalReference,
            Subtotal = cart.Subtotal,
            TaxRate = cart.TaxRate,
            TaxAmount = cart.TaxAmount,
            Total = cart.Total,
            Status = "Pagado"
        };

        foreach (var item in dto.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken)!;
            if (product!.Stock < item.Quantity)
            {
                throw new ValidationException($"Stock insuficiente para el producto '{product.Name}'. Stock actual: {product.Stock}, solicitado: {item.Quantity}.");
            }

            product.Stock -= item.Quantity;
            _productRepository.Update(product);

            sale.Details.Add(new SaleDetail
            {
                SaleId = sale.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = product.UnitPrice * item.Quantity
            });
        }

        await _saleRepository.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saleDto = _mapper.Map<SaleDto>(sale);
        saleDto.CustomerName = $"{customer.FirstName} {customer.LastName}".Trim();
        saleDto.CustomerEmail = customer.Email;

        // Generar recibo en PDF y enviar notificación por correo electrónico (S2 y S4)
        try
        {
            var pdfBytes = await _pdfService.GenerateSaleReceiptPdfAsync(saleDto, cancellationToken);

            var receiptsDir = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "recibos");
            if (!Directory.Exists(receiptsDir))
            {
                Directory.CreateDirectory(receiptsDir);
            }

            var receiptPath = Path.Combine(receiptsDir, $"{sale.Id}.pdf");
            await File.WriteAllBytesAsync(receiptPath, pdfBytes, cancellationToken);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                var emailSubject = $"Comprobante de compra - Factura #{sale.Id.ToString()[..8].ToUpper()}";
                var emailBody = $@"
                    <h2>¡Gracias por su compra en Firmeza!</h2>
                    <p>Estimado/a <strong>{customer.FirstName} {customer.LastName}</strong>,</p>
                    <p>Su compra por un valor total de <strong>${sale.Total:N2}</strong> ha sido procesada exitosamente.</p>
                    <p>Adjuntamos el comprobante de pago oficial en formato PDF.</p>
                ";

                await _emailService.SendEmailAsync(
                    customer.Email,
                    emailSubject,
                    emailBody,
                    pdfBytes,
                    $"Recibo-{sale.Id}.pdf",
                    cancellationToken);
            }
        }
        catch
        {
            // Falla de notificación externa no cancela la persistencia de la transacción
        }

        return saleDto;
    }

    public async Task<SaleDto> GetSaleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException($"La venta con ID {id} no fue encontrada.");

        return _mapper.Map<SaleDto>(sale);
    }

    public async Task<IReadOnlyList<SaleDto>> GetAllSalesAsync(Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var sales = await _saleRepository.GetAllAsync(customerId, cancellationToken);
        return _mapper.Map<IReadOnlyList<SaleDto>>(sales);
    }

    public async Task<SaleDto> CancelSaleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException($"La venta con ID {id} no fue encontrada.");

        if (sale.Status == "Cancelado")
        {
            throw new ValidationException("La venta ya se encuentra en estado cancelado.");
        }

        // Devolver stock de los productos
        foreach (var detail in sale.Details)
        {
            var product = await _productRepository.GetByIdAsync(detail.ProductId, cancellationToken);
            if (product != null)
            {
                product.Stock += detail.Quantity;
                _productRepository.Update(product);
            }
        }

        sale.Status = "Cancelado";
        _saleRepository.Update(sale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SaleDto>(sale);
    }

    public async Task<byte[]> GetSaleReceiptBytesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var receiptsDir = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "recibos");
        var receiptPath = Path.Combine(receiptsDir, $"{id}.pdf");

        if (File.Exists(receiptPath))
        {
            return await File.ReadAllBytesAsync(receiptPath, cancellationToken);
        }

        // Si el archivo no está en disco, se regenera con los datos de la venta
        var sale = await GetSaleByIdAsync(id, cancellationToken);
        var pdfBytes = await _pdfService.GenerateSaleReceiptPdfAsync(sale, cancellationToken);

        if (!Directory.Exists(receiptsDir))
        {
            Directory.CreateDirectory(receiptsDir);
        }

        await File.WriteAllBytesAsync(receiptPath, pdfBytes, cancellationToken);
        return pdfBytes;
    }
}

public class RentalService : IRentalService
{
    private readonly IRentalRepository _rentalRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RentalService(
        IRentalRepository rentalRepository,
        IVehicleRepository vehicleRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _rentalRepository = rentalRepository;
        _vehicleRepository = vehicleRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<RentalQuoteDto> CalculateRentalQuoteAsync(Guid vehicleId, DateTime startDate, DateTime endDate, decimal taxRate = 0.19m, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new NotFoundException($"El vehículo con ID {vehicleId} no fue encontrado.");

        var days = (int)Math.Ceiling((endDate - startDate).TotalDays);
        if (days <= 0) days = 1;

        var subtotal = vehicle.DailyRate * days;
        var taxAmount = Math.Round(subtotal * taxRate, 2);
        var total = subtotal + taxAmount;

        return new RentalQuoteDto
        {
            VehicleId = vehicle.Id,
            DaysRented = days,
            DailyRate = vehicle.DailyRate,
            Subtotal = subtotal,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = total
        };
    }

    public async Task<RentalDto> CreateRentalAsync(CreateRentalDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId, cancellationToken)
            ?? throw new NotFoundException($"El cliente con ID {dto.CustomerId} no existe.");

        var vehicle = await _vehicleRepository.GetByIdAsync(dto.VehicleId, cancellationToken)
            ?? throw new NotFoundException($"El vehículo con ID {dto.VehicleId} no existe.");

        if (vehicle.Status != VehicleStatus.Available)
        {
            throw new ValidationException($"El vehículo '{vehicle.Brand} {vehicle.Model}' no se encuentra disponible (Estado: {vehicle.Status}).");
        }

        var hasCollision = await _rentalRepository.HasOverlappingRentalAsync(dto.VehicleId, dto.StartDate, dto.EndDate, null, cancellationToken);
        if (hasCollision)
        {
            throw new ValidationException("El vehículo ya tiene una reserva en el rango de fechas seleccionado.");
        }

        var quote = await CalculateRentalQuoteAsync(dto.VehicleId, dto.StartDate, dto.EndDate, 0.19m, cancellationToken);

        var rental = new Rental
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DaysRented = quote.DaysRented,
            DailyRate = quote.DailyRate,
            Subtotal = quote.Subtotal,
            TaxRate = quote.TaxRate,
            TaxAmount = quote.TaxAmount,
            Total = quote.Total,
            Status = RentalStatus.Active,
            Notes = dto.Notes
        };

        vehicle.Status = VehicleStatus.Rented;
        _vehicleRepository.Update(vehicle);

        await _rentalRepository.AddAsync(rental, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<RentalDto>(rental);
    }

    public async Task<RentalDto> GetRentalByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rental = await _rentalRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"El contrato de alquiler con ID {id} no fue encontrado.");

        return _mapper.Map<RentalDto>(rental);
    }

    public async Task<IReadOnlyList<RentalDto>> GetAllRentalsAsync(Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var rentals = await _rentalRepository.GetAllAsync(customerId, null, cancellationToken);
        return _mapper.Map<IReadOnlyList<RentalDto>>(rentals);
    }

    public async Task<RentalDto> CancelRentalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rental = await _rentalRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"El contrato de alquiler con ID {id} no fue encontrado.");

        if (rental.Status == RentalStatus.Cancelled)
        {
            throw new ValidationException("El contrato de alquiler ya se encuentra cancelado.");
        }

        rental.Status = RentalStatus.Cancelled;
        _rentalRepository.Update(rental);

        var vehicle = await _vehicleRepository.GetByIdAsync(rental.VehicleId, cancellationToken);
        if (vehicle != null)
        {
            vehicle.Status = VehicleStatus.Available;
            _vehicleRepository.Update(vehicle);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<RentalDto>(rental);
    }
}

public class DashboardService : IDashboardService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IRentalRepository _rentalRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public DashboardService(
        ISaleRepository saleRepository,
        IRentalRepository rentalRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IVehicleRepository vehicleRepository)
    {
        _saleRepository = saleRepository;
        _rentalRepository = rentalRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default)
    {
        var salesRevenue = await _saleRepository.GetTotalSalesRevenueAsync(cancellationToken);
        var salesCount = await _saleRepository.GetTotalSalesCountAsync(cancellationToken);
        var rentalsRevenue = await _rentalRepository.GetTotalRentalsRevenueAsync(cancellationToken);
        var activeRentals = await _rentalRepository.GetActiveRentalsCountAsync(cancellationToken);
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        var lowStock = await _productRepository.GetLowStockAsync(cancellationToken);
        var availableVehicles = await _vehicleRepository.GetAllAsync(VehicleStatus.Available, null, cancellationToken);

        return new DashboardMetricsDto
        {
            TotalSalesRevenue = salesRevenue,
            TotalSalesCount = salesCount,
            TotalRentalsRevenue = rentalsRevenue,
            ActiveRentalsCount = activeRentals,
            TotalCustomersCount = customers.Count,
            LowStockProductsCount = lowStock.Count,
            AvailableVehiclesCount = availableVehicles.Count
        };
    }
}
