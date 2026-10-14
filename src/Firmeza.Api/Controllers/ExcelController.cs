using Firmeza.Application.DTOs.Excel;
using Firmeza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public class ExcelController : ControllerBase
{
    private readonly IExcelService _excelService;
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

    public ExcelController(IExcelService excelService)
    {
        _excelService = excelService;
    }

    [HttpPost("import-products")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportProducts(IFormFile? file, CancellationToken ct)
    {
        var validationError = ValidateExcelFile(file);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        using var stream = file!.OpenReadStream();
        var result = await _excelService.ImportProductsFromExcelAsync(stream, ct);
        return Ok(result);
    }

    [HttpPost("import-customers")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportCustomers(IFormFile? file, CancellationToken ct)
    {
        var validationError = ValidateExcelFile(file);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        using var stream = file!.OpenReadStream();
        var result = await _excelService.ImportCustomersFromExcelAsync(stream, ct);
        return Ok(result);
    }

    [HttpGet("export-sales")]
    public async Task<IActionResult> ExportSales(CancellationToken ct)
    {
        var bytes = await _excelService.ExportSalesToExcelAsync(ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Ventas-Firmeza-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx");
    }

    private static string? ValidateExcelFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "El archivo Excel es obligatorio y no puede estar vacío.";
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            return "El tamaño del archivo no puede superar los 10 MB.";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            return "Formato de archivo no válido. Solo se admiten archivos con extensión '.xlsx'.";
        }

        return null;
    }
}
