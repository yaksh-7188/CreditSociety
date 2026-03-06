using Microsoft.AspNetCore.Mvc;

namespace CreditSociety.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    [HttpGet("members")]
    public IActionResult GetMembers()
    {
        var members = new[]
        {
            new { id = 1, name = "John Doe", email = "john@email.com" },
            new { id = 2, name = "Jane Smith", email = "jane@email.com" }
        };
        
        return Ok(members);
    }
}