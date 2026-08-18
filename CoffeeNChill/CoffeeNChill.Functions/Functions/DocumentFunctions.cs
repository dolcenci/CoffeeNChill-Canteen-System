using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CoffeeNChill.Functions.Services;

namespace CoffeeNChill.Functions.Functions
{
    public class DocumentFunctions
    {
        private readonly DocumentService _documentService;
        private readonly ILogger _logger;

        public DocumentFunctions(DocumentService documentService, ILogger<DocumentFunctions> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> UploadStaffDocument(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "documents/upload")] HttpRequestData req)
        {
            try
            {
                // Read the request body as a stream
                using var memoryStream = new MemoryStream();
                await req.Body.CopyToAsync(memoryStream);

                // For multipart form data, we need to handle it differently
                // We'll use the stream directly
                var contentType = req.Headers.GetValues("Content-Type");
                var boundary = GetBoundary(contentType);

                if (string.IsNullOrEmpty(boundary))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid content type. Must be multipart/form-data");
                    return badResponse;
                }

                // Simple approach: read the file from the stream
                // For now, we'll save the file with a timestamp
                var fileName = $"upload_{DateTime.Now:yyyyMMddHHmmss}.dat";

                // Reset stream position
                memoryStream.Position = 0;
                await _documentService.UploadDocumentAsync(fileName, memoryStream);

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"File uploaded successfully: {fileName}");
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UploadStaffDocument: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        private string GetBoundary(IEnumerable<string> contentTypeValues)
        {
            if (contentTypeValues == null)
                return null;

            // Convert to array to work with it
            var contentTypes = new List<string>(contentTypeValues);

            foreach (var ct in contentTypes)
            {
                if (ct.Contains("multipart/form-data"))
                {
                    var parts = ct.Split(';');
                    foreach (var part in parts)
                    {
                        if (part.Trim().StartsWith("boundary="))
                        {
                            return part.Trim().Substring("boundary=".Length);
                        }
                    }
                }
            }
            return null;
        }

        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> ListStaffDocuments(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "documents")] HttpRequestData req)
        {
            try
            {
                var documents = await _documentService.ListDocumentsAsync();
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);

                // Manual JSON serialization
                var jsonString = System.Text.Json.JsonSerializer.Serialize(documents);
                await response.WriteStringAsync(jsonString);
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ListStaffDocuments: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DownloadStaffDocument")]
        public async Task<HttpResponseData> DownloadStaffDocument(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "documents/download/{fileName}")] HttpRequestData req,
            string fileName)
        {
            try
            {
                var stream = await _documentService.DownloadDocumentAsync(fileName);

                if (stream == null)
                {
                    var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Document '{fileName}' not found");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/octet-stream");
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

                // Convert stream to byte array
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                await response.WriteBytesAsync(memoryStream.ToArray());

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DownloadStaffDocument: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}