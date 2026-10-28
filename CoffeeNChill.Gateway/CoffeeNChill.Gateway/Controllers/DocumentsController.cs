using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoffeeNChill.Gateway.Services;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Document management endpoints (staff-only).
/// </summary>
[ApiController]
[Route("api/documents")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly AzureFunctionService _functionService;

    public DocumentsController(AzureFunctionService functionService)
    {
        _functionService = functionService;
    }

    /// <summary>
    /// Lists all staff documents. Requires Admin or Staff role.
    /// </summary>
    /// <returns>List of documents with name, size, and last modified date.</returns>
    /// <response code="200">Documents returned successfully.</response>
    /// <response code="401">Unauthorized - JWT token required.</response>
    /// <response code="403">Forbidden - Admin or Staff role required.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListDocuments()
    {
        var result = await _functionService.ListDocumentsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Uploads a staff document. Requires Admin or Staff role.
    /// </summary>
    /// <param name="file">The file to upload (multipart/form-data).</param>
    /// <returns>Upload confirmation with file name.</returns>
    /// <response code="200">File uploaded successfully.</response>
    /// <response code="400">No file uploaded.</response>
    /// <response code="401">Unauthorized - JWT token required.</response>
    /// <response code="403">Forbidden - Admin or Staff role required.</response>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        using var stream = file.OpenReadStream();
        var result = await _functionService.UploadDocumentAsync(stream, file.FileName);
        return Ok(result);
    }

    /// <summary>
    /// Downloads a staff document by file name. Requires Admin or Staff role.
    /// </summary>
    /// <param name="fileName">The name of the file to download.</param>
    /// <returns>The file as a download stream.</returns>
    /// <response code="200">File downloaded successfully.</response>
    /// <response code="404">File not found.</response>
    /// <response code="401">Unauthorized - JWT token required.</response>
    /// <response code="403">Forbidden - Admin or Staff role required.</response>
    [HttpGet("download/{fileName}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadDocument(string fileName)
    {
        var (stream, name) = await _functionService.DownloadDocumentAsync(fileName);
        if (stream == null)
            return NotFound(new { message = $"Document '{fileName}' not found." });

        return File(stream, "application/octet-stream", name);
    }
}