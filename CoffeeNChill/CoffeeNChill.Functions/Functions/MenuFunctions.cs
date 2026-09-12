using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CoffeeNChill.Functions.Services;

namespace CoffeeNChill.Functions.Functions
{
    public class MenuFunctions
    {
        private readonly MenuService _menuService;
        private readonly ILogger _logger;

        public MenuFunctions(MenuService menuService, ILogger<MenuFunctions> logger)
        {
            _menuService = menuService;
            _logger = logger;
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> CreateMenuItem(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "menu")] HttpRequestData req)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var menuItem = JsonSerializer.Deserialize<MenuItem>(requestBody);

                if (menuItem == null)
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid menu item data");
                    return badResponse;
                }

                if (string.IsNullOrEmpty(menuItem.PartitionKey))
                    menuItem.PartitionKey = "General";

                if (string.IsNullOrEmpty(menuItem.RowKey))
                    menuItem.RowKey = Guid.NewGuid().ToString();

                var result = await _menuService.CreateMenuItem(menuItem);

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                var jsonString = JsonSerializer.Serialize(result);
                await response.WriteStringAsync(jsonString);
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateMenuItem: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> GetAllMenuItems(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "menu")] HttpRequestData req)
        {
            try
            {
                var items = await _menuService.GetAllMenuItems();
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                var jsonString = JsonSerializer.Serialize(items);
                await response.WriteStringAsync(jsonString);
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllMenuItems: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> GetMenuItemsByCategory(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "menu/category/{category}")] HttpRequestData req,
            string category)
        {
            try
            {
                var items = await _menuService.GetMenuItemsByCategory(category);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                var jsonString = JsonSerializer.Serialize(items);
                await response.WriteStringAsync(jsonString);
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetMenuItemsByCategory: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> UpdateMenuItem(
     [HttpTrigger(AuthorizationLevel.Function, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
     string category, string id)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(requestBody);

                double? price = null;
                bool? isAvailable = null;

                if (updateData.ContainsKey("Price") && updateData["Price"].ValueKind == JsonValueKind.Number)
                {
                    price = updateData["Price"].GetDouble();
                }

                if (updateData.ContainsKey("IsAvailable") &&
                    (updateData["IsAvailable"].ValueKind == JsonValueKind.True ||
                     updateData["IsAvailable"].ValueKind == JsonValueKind.False))
                {
                    isAvailable = updateData["IsAvailable"].GetBoolean();
                }

                var result = await _menuService.UpdateMenuItem(category, id, price, isAvailable);

                if (result == null)
                {
                    var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Menu item with category '{category}' and id '{id}' not found");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                var jsonString = JsonSerializer.Serialize(result);
                await response.WriteStringAsync(jsonString);
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateMenuItem: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> DeleteMenuItem(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            try
            {
                var result = await _menuService.DeleteMenuItem(category, id);

                if (!result)
                {
                    var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Menu item with category '{category}' and id '{id}' not found");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"Menu item with category '{category}' and id '{id}' deleted successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteMenuItem: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}