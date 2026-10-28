using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoffeeNChill.Gateway.Services;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Order management endpoints.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly AzureFunctionService _functionService;

    public OrdersController(AzureFunctionService functionService)
    {
        _functionService = functionService;
    }

    /// <summary>
    /// Places a new order into the processing queue. Public access.
    /// </summary>
    /// <param name="order">Order details including OrderId, CustomerName, SelectedItemSKUs, TotalPrice, OrderTimestamp.</param>
    /// <returns>Confirmation with the order ID.</returns>
    /// <response code="200">Order placed successfully.</response>
    /// <response code="400">Invalid order data.</response>
    [HttpPost("queue")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder([FromBody] object order)
    {
        var result = await _functionService.PlaceOrderAsync(order);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all orders. Requires Admin or Staff role.
    /// </summary>
    /// <returns>List of all orders with their current status.</returns>
    /// <response code="200">Orders returned successfully.</response>
    /// <response code="401">Unauthorized - JWT token required.</response>
    /// <response code="403">Forbidden - Admin or Staff role required.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders()
    {
        var result = await _functionService.GetAllOrdersAsync();
        return Ok(result);
    }

    /// <summary>
    /// Updates the status of an order (e.g., Preparing, Ready, Collected). Requires Admin or Staff role.
    /// </summary>
    /// <param name="orderId">The unique identifier of the order.</param>
    /// <param name="statusUpdate">New status value.</param>
    /// <returns>Updated order.</returns>
    /// <response code="200">Order status updated successfully.</response>
    /// <response code="401">Unauthorized - JWT token required.</response>
    /// <response code="403">Forbidden - Admin or Staff role required.</response>
    [HttpPut("status")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult UpdateOrderStatus([FromBody] object statusUpdate)
    {
        // Note: real update goes through the Orders table in Part 2.
        // This endpoint is a placeholder that acknowledges the request.
        return Ok(new { message = "Order status update received", data = statusUpdate });
    }
}