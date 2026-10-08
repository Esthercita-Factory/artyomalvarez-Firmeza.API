namespace Firmeza.Application.DTOs.Dashboard;

public class DashboardMetricsDto
{
    public decimal TotalSalesRevenue { get; set; }
    public int TotalSalesCount { get; set; }
    public decimal TotalRentalsRevenue { get; set; }
    public int ActiveRentalsCount { get; set; }
    public int TotalCustomersCount { get; set; }
    public int LowStockProductsCount { get; set; }
    public int AvailableVehiclesCount { get; set; }
}
