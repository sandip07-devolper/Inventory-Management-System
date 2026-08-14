using InventoryOrderSystem.API.DTOs.Customers;
using InventoryOrderSystem.API.Services.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
        => Ok(await _customerService.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
        => Ok(await _customerService.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request)
    {
        var customer = await _customerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, UpdateCustomerRequest request)
        => Ok(await _customerService.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        return NoContent();
    }
}
