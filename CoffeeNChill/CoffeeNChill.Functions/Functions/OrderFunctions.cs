using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CoffeeNChill.Functions.Services;

namespace CoffeeNChill.Functions.Functions
{
    public class OrderFunctions
    {
        private readonly OrderService _orderService;
        private readonly ILogger _logger;

        public OrderFunctions(OrderService orderService, ILogger<OrderFunctions> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [Function("PlaceOrder")]
        public async Task<HttpResponseData> PlaceOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/queue")] HttpRequestData req)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var order = JsonSerializer.Deserialize<Order>(requestBody);

                if (order == null)
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid order data");
                    return badResponse;
                }

                // Add order to queue
                var orderId = await _orderService.AddOrderToQueueAsync(order);

                // WriteAsJsonAsync sets Content-Type automatically — do NOT add it manually
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    Message = "Order placed successfully",
                    OrderId = orderId,
                    Status = "Received (Processing)"
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in PlaceOrder: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("ProcessOrderQueue")]
        public async Task ProcessOrderQueue(
            [QueueTrigger("order-processing-queue")] string queueMessage)
        {
            try
            {
                _logger.LogInformation($"Processing order from queue: {queueMessage}");

                // Process the order
                var orderEntity = await _orderService.ProcessOrderFromQueueAsync(queueMessage);

                // Simulate status updates
                await SimulateOrderStatusUpdates(orderEntity.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ProcessOrderQueue: {ex.Message}");
                throw;
            }
        }

        private async Task SimulateOrderStatusUpdates(string orderId)
        {
            try
            {
                // Simulate Preparing status
                await Task.Delay(5000); // Wait 5 seconds
                await _orderService.UpdateOrderStatusAsync(orderId, "Preparing");
                _logger.LogInformation($"Order {orderId} status: Preparing");

                // Simulate Ready status
                await Task.Delay(8000); // Wait 8 seconds
                await _orderService.UpdateOrderStatusAsync(orderId, "Ready");
                _logger.LogInformation($"Order {orderId} status: Ready");

                // Simulate Collected status
                await Task.Delay(10000); // Wait 10 seconds
                await _orderService.UpdateOrderStatusAsync(orderId, "Collected");
                _logger.LogInformation($"Order {orderId} status: Collected");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error simulating status updates: {ex.Message}");
                throw;
            }
        }

        [Function("GetAllOrders")]
        public async Task<HttpResponseData> GetAllOrders(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequestData req)
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(orders);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllOrders: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}