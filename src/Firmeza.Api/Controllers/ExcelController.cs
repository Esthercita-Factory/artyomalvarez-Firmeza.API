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

        try
        {
            using var stream = file!.OpenReadStream();
            var result = await _excelService.ImportProductsFromExcelAsync(stream, ct);
            return Ok(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return BadRequest(new { message = "Error al procesar el archivo Excel. Asegúrese de que el formato interno sea válido y no esté dañado.", error = ex.Message });
        }
    }

    [HttpPost("import-customers")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportCustomers(IFormFile? file, CancellationToken ct)
    {
        var validationError = ValidateExcelFile(file);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        try
        {
            using var stream = file!.OpenReadStream();
            var result = await _excelService.ImportCustomersFromExcelAsync(stream, ct);
            return Ok(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return BadRequest(new { message = "Error al procesar el archivo Excel. Asegúrese de que el formato interno sea válido y no esté dañado.", error = ex.Message });
        }
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

        // Inspección de cabecera mágica (ZIP header 'PK\x03\x04') para detectar archivos renombrados (.txt a .xlsx)
        try
        {
            using var stream = file.OpenReadStream();
            var header = new byte[4];
            int read = stream.Read(header, 0, 4);
            if (read < 4 || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
            {
                return "El archivo no contiene una estructura válida de libro de Excel (.xlsx). Compruebe que no sea un archivo renombrado.";
            }
        }
        catch
        {
            return "No se pudo leer la cabecera del archivo para validar su formato.";
        }

        return null;
    }
}
