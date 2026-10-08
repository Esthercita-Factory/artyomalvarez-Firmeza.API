using AutoMapper;
using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.DTOs.Products;
using Firmeza.Application.DTOs.Rentals;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.DTOs.Vehicles;
using Firmeza.Domain.Entities;

namespace Firmeza.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Products
        CreateMap<Product, ProductDto>();
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        // Customers
        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateCustomerDto, Customer>();
        CreateMap<UpdateCustomerDto, Customer>();

        // Vehicles
        CreateMap<Vehicle, VehicleDto>();
        CreateMap<CreateVehicleDto, Vehicle>();
        CreateMap<UpdateVehicleDto, Vehicle>();

        // Rentals
        CreateMap<Rental, RentalDto>()
            .MaxDepth(3)
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}".Trim() : string.Empty))
            .ForMember(dest => dest.VehiclePlate, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.Plate : string.Empty))
            .ForMember(dest => dest.VehicleModel, opt => opt.MapFrom(src => src.Vehicle != null ? $"{src.Vehicle.Brand} {src.Vehicle.Model}".Trim() : string.Empty));

        // Sales
        CreateMap<Sale, SaleDto>()
            .MaxDepth(3)
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}".Trim() : string.Empty))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Email : string.Empty));

        CreateMap<SaleDetail, SaleDetailDto>()
            .MaxDepth(3)
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
            .ForMember(dest => dest.ProductSku, opt => opt.MapFrom(src => src.Product != null ? src.Product.Sku : string.Empty));

    }
}
