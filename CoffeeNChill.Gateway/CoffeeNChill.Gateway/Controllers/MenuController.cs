using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoffeeNChill.Gateway.Services;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Menu management endpoints. Public access for viewing, restricted for modification.
/// </summary>
[ApiController]
[Route("api/menu")]
[Produces("application/json")]
public class MenuController : ControllerBase
{
    private readonly AzureFunctionService _functionService;

    public MenuController(AzureFunctionService functionService)
    {
        _functionService = functionService;
    }

    /// <summary>
    /// Retrieves all menu items. Public access.
    /// </summary>
    /// <returns>List of menu items.</returns>
    /// <response code="200">Menu items returned successfully.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _functionService.GetMenuItemsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Retrieves menu items by category. Public access.
    /// </summary>
    /// <param name="category">The category to filter by (e.g., "Hot Drinks").</param>
    /// <returns>List of menu items in the category.</returns>
    /// <response code="200">Menu items returned successfully.</response>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var result = await _functionService.GetMenuItemsByCategoryAsync(category);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new menu item. Requires Admin or Staff role.
    /// </summary>
    /// <param name="menuItem">Menu item details.</param>
    /// <returns>Created menu item.</returns>
    /// <response code="201">Menu item created successfully.</response>
    /// <response code="401">Unauthorized - JWT token required.</response>
    /// <response code="403">Forbidden - Admin or Staff role required.</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] object menuItem)
    {
        var result = await _functionService.CreateMenuItemAsync(menuItem);
        return CreatedAtAction(nameof(GetAll), result);
    }

    /// <summary>
    /// Updates an existing menu item. Requires Admin or Staff role.
    /// </summary>
    /// <param name="category">The category of the menu item.</param>
    /// <param name="id">The unique identifier (RowKey) of the menu item.</param>
    /// <param name="updateData">Fields to update (Price, IsAvailable).</param>
    /// <returns>Updated menu item.</returns>
    /// <response code="200">Menu item updated successfully.</response>
    /// <response code="401">Unauthorized - JWT token required.</response>
    /// <response code="403">Forbidden - Admin or Staff role required.</response>
    /// <response code="404">Menu item not found.</response>
    [HttpPut("{category}/{id}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string category, string id, [FromBody] object updateData)
    {
        var result = await _functionService.UpdateMenuItemAsync(category, id, updateData);
        if (result == null)
            return NotFound(new { message = $"Menu item with category '{category}' and id '{id}' not found." });
        return Ok(result);
    }

    /// <summary>
    /// Deletes a menu item. Requires Admin or Staff role.
    /// </summary>
    /// <param name="category">The category of the menu item.</param>
    /// <param name="id">The unique identifier (RowKey) of the menu item.</param>
    /// <returns>Success message.</returns>
    /// <response code="200">Menu item deleted successfully.</response>
    /// <response code="401">Unauthorized - JWT token required.</response>
    /// <response code="403">Forbidden - Admin or Staff role required.</response>
    /// <response code="404">Menu item not found.</response>
    [HttpDelete("{category}/{id}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string category, string id)
    {
        var result = await _functionService.DeleteMenuItemAsync(category, id);
        if (!result)
            return NotFound(new { message = $"Menu item with category '{category}' and id '{id}' not found." });
        return Ok(new { message = "Menu item deleted successfully." });
    }
}