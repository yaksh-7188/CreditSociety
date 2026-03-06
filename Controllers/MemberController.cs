using Microsoft.AspNetCore.Mvc;

namespace CreditSociety.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MemberController : ControllerBase
{
    [HttpGet("emi/{userId}")]
    public IActionResult GetEMI(int userId)
    {
        var emi = new
        {
            totalAmount = 100000,
            paidAmount = 25000,
            remainingAmount = 75000,
            interestRate = 8.5
        };
        
        return Ok(emi);
    }
}