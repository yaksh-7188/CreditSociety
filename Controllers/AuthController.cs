using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using CreditSociety;

namespace CreditSociety.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login(LoginModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = "SELECT * FROM creditsocietydb_users WHERE Username = @Username AND Password = @Password";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Username", model.Username);
            cmd.Parameters.AddWithValue("@Password", model.Password);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return Ok(new
                {
                    userId = reader["Id"],
                    username = reader["Username"],
                    fullName = reader["FullName"],
                    role = reader["Role"]
                });
            }
            
            return Unauthorized(new { message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = "SELECT * FROM creditsocietydb_users";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            
            var users = new List<object>();
            while (reader.Read())
            {
                users.Add(new
                {
                    id = reader["Id"],
                    username = reader["Username"],
                    fullName = reader["FullName"],
                    email = reader["Email"],
                    phone = reader["Phone"],
                    role = reader["Role"]
                });
            }
            
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpPost("users")]
    public IActionResult AddUser(UserModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                INSERT INTO creditsocietydb_users (Username, Password, FullName, Email, Phone, Role) 
                VALUES (@Username, @Password, @FullName, @Email, @Phone, @Role)";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Username", model.Username);
            cmd.Parameters.AddWithValue("@Password", model.Password);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@Phone", model.Phone);
            cmd.Parameters.AddWithValue("@Role", model.Role);
            
            cmd.ExecuteNonQuery();
            return Ok(new { message = "User added successfully" });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return BadRequest(new { message = "Username already exists!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpDelete("users/{id}")]
    public IActionResult DeleteUser(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = "DELETE FROM creditsocietydb_users WHERE Id = @Id AND Role != 'Admin'";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                return Ok(new { message = "User deleted successfully" });
            }
            else
            {
                return BadRequest(new { message = "Cannot delete admin user or user not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpGet("loans")]
    public IActionResult GetLoans()
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                SELECT l.LoanID, l.LoanAmount, l.InterestRate, l.Status, u.FullName as MemberName
                FROM creditsocietydb_loans l
                JOIN creditsocietydb_users u ON l.UserID = u.Id";
                
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            
            var loans = new List<object>();
            while (reader.Read())
            {
                loans.Add(new
                {
                    loanId = reader["LoanID"],
                    memberName = reader["MemberName"],
                    amount = reader["LoanAmount"],
                    interest = reader["InterestRate"],
                    status = reader["Status"]
                });
            }
            
            return Ok(loans);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpPost("loans")]
    public IActionResult AddLoan(LoanModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                INSERT INTO creditsocietydb_loans (UserID, LoanAmount, InterestRate, LoanTerm, StartDate, EndDate, Status) 
                VALUES (@UserID, @LoanAmount, @InterestRate, @LoanTerm, @StartDate, @EndDate, @Status)";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserID", model.UserID);
            cmd.Parameters.AddWithValue("@LoanAmount", model.LoanAmount);
            cmd.Parameters.AddWithValue("@InterestRate", model.InterestRate);
            cmd.Parameters.AddWithValue("@LoanTerm", model.LoanTerm);
            cmd.Parameters.AddWithValue("@StartDate", model.StartDate);
            cmd.Parameters.AddWithValue("@EndDate", model.EndDate);
            cmd.Parameters.AddWithValue("@Status", model.Status);
            
            cmd.ExecuteNonQuery();
            return Ok(new { message = "Loan added successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpDelete("loans/{id}")]
    public IActionResult DeleteLoan(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = "DELETE FROM creditsocietydb_loans WHERE LoanID = @Id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                return Ok(new { message = "Loan deleted successfully" });
            }
            else
            {
                return BadRequest(new { message = "Loan not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpGet("emi")]
    public IActionResult GetEMI()
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                SELECT e.*, u.FullName as MemberName 
                FROM creditsocietydb_emi e
                JOIN creditsocietydb_users u ON e.UserID = u.Id";
                
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            
            var emiList = new List<object>();
            while (reader.Read())
            {
                emiList.Add(new
                {
                    emiId = reader["EMIID"],
                    memberName = reader["MemberName"],
                    totalAmount = reader["TotalAmount"],
                    paidAmount = reader["PaidAmount"],
                    remainingAmount = reader["RemainingAmount"],
                    interestRate = reader["InterestRate"],
                    startDate = reader["StartDate"],
                    endDate = reader["EndDate"],
                    status = reader["Status"]
                });
            }
            
            return Ok(emiList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpPost("emi")]
    public IActionResult AddEMI(EMIModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                INSERT INTO creditsocietydb_emi (UserID, TotalAmount, PaidAmount, RemainingAmount, InterestRate, StartDate, EndDate, Status) 
                VALUES (@UserID, @TotalAmount, @PaidAmount, @RemainingAmount, @InterestRate, @StartDate, @EndDate, @Status)";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserID", model.UserID);
            cmd.Parameters.AddWithValue("@TotalAmount", model.TotalAmount);
            cmd.Parameters.AddWithValue("@PaidAmount", model.PaidAmount);
            cmd.Parameters.AddWithValue("@RemainingAmount", model.RemainingAmount);
            cmd.Parameters.AddWithValue("@InterestRate", model.InterestRate);
            cmd.Parameters.AddWithValue("@StartDate", model.StartDate);
            cmd.Parameters.AddWithValue("@EndDate", model.EndDate);
            cmd.Parameters.AddWithValue("@Status", model.Status);
            
            cmd.ExecuteNonQuery();
            return Ok(new { message = "EMI added successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpDelete("emi/{id}")]
    public IActionResult DeleteEMI(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = "DELETE FROM creditsocietydb_emi WHERE EMIID = @Id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                return Ok(new { message = "EMI deleted successfully" });
            }
            else
            {
                return BadRequest(new { message = "EMI not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }
}

public class LoginModel
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class UserModel
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "Member";
}

public class LoanModel
{
    public int UserID { get; set; }
    public decimal LoanAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int LoanTerm { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active";
}

public class EMIModel
{
    public int UserID { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active";
}