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
                VALUES (@Username, @Password, @FullName, @Email, @Phone, @Role);
                SELECT LAST_INSERT_ID();";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Username", model.Username);
            cmd.Parameters.AddWithValue("@Password", model.Password);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@Phone", model.Phone);
            cmd.Parameters.AddWithValue("@Role", model.Role);
            
            int newUserId = Convert.ToInt32(cmd.ExecuteScalar());
            
            // Auto EMI for new member (3 months Pending)
            string today = DateTime.Now.ToString("yyyy-MM-01");
            string prevMonth1 = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-01");
            string prevMonth2 = DateTime.Now.AddMonths(-2).ToString("yyyy-MM-01");
            
            string emiQuery = @"
                INSERT INTO monthly_emi (member_id, month_year, amount, paid_amount, status) VALUES
                (@uid, @month1, 1000, 0, 'Pending'),
                (@uid, @month2, 1000, 0, 'Pending'),
                (@uid, @month3, 1000, 0, 'Pending')";
                
            using var emiCmd = new MySqlCommand(emiQuery, conn);
            emiCmd.Parameters.AddWithValue("@uid", newUserId);
            emiCmd.Parameters.AddWithValue("@month1", today);
            emiCmd.Parameters.AddWithValue("@month2", prevMonth1);
            emiCmd.Parameters.AddWithValue("@month3", prevMonth2);
            emiCmd.ExecuteNonQuery();
            
            return Ok(new { message = "Member added successfully", userId = newUserId });
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
                SELECT 
                    l.LoanID, 
                    l.UserID,
                    l.LoanAmount, 
                    l.InterestRate, 
                    l.LoanTerm,
                    l.StartDate,
                    l.EndDate,
                    l.Status,
                    l.PaidAmount,
                    l.ClosingDate,
                    l.TotalInterest,
                    l.TotalPayable,
                    l.IsClosed,
                    u.FullName as MemberName 
                FROM creditsocietydb_loans l
                LEFT JOIN creditsocietydb_users u ON l.UserID = u.Id
                ORDER BY l.LoanID DESC";
                
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            
            var loans = new List<object>();
            while (reader.Read())
            {
                loans.Add(new
                {
                    loanId = reader["LoanID"],
                    userID = Convert.ToInt32(reader["UserID"]),
                    memberName = reader["MemberName"]?.ToString() ?? "Unknown",
                    amount = Convert.ToDecimal(reader["LoanAmount"]),
                    interest = Convert.ToDecimal(reader["InterestRate"]),
                    term = Convert.ToInt32(reader["LoanTerm"]),
                    startDate = reader["StartDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd"),
                    endDate = reader["EndDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-dd"),
                    status = reader["Status"]?.ToString() ?? "Active",
                    paidAmount = reader["PaidAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PaidAmount"]),
                    closingDate = reader["ClosingDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ClosingDate"]).ToString("yyyy-MM-dd"),
                    totalInterest = reader["TotalInterest"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalInterest"]),
                    totalPayable = reader["TotalPayable"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalPayable"]),
                    isClosed = reader["IsClosed"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsClosed"])
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
            
            // Parse start date safely
            DateTime startDate;
            if (!DateTime.TryParse(model.StartDate, out startDate))
            {
                return BadRequest(new { message = "Invalid start date format" });
            }
            
            DateTime endDate = startDate.AddMonths(model.LoanTerm);

            // FIX: TotalInterest aur TotalPayable calculate karke DB mein save karo
            decimal totalInterest = model.LoanAmount * model.InterestRate / 100;
            decimal totalPayable = model.LoanAmount + totalInterest;
            
            string query = @"
                INSERT INTO creditsocietydb_loans 
                (UserID, LoanAmount, InterestRate, LoanTerm, StartDate, EndDate, Status, PaidAmount, TotalInterest, TotalPayable, IsClosed) 
                VALUES 
                (@UserID, @LoanAmount, @InterestRate, @LoanTerm, @StartDate, @EndDate, 'Active', 0, @TotalInterest, @TotalPayable, FALSE)";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserID", model.UserID);
            cmd.Parameters.AddWithValue("@LoanAmount", model.LoanAmount);
            cmd.Parameters.AddWithValue("@InterestRate", model.InterestRate);
            cmd.Parameters.AddWithValue("@LoanTerm", model.LoanTerm);
            cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@TotalInterest", totalInterest);
            cmd.Parameters.AddWithValue("@TotalPayable", totalPayable);
            
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

    [HttpPut("loans/{id}/close")]
    public IActionResult CloseLoan(int id, [FromBody] CloseLoanModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                UPDATE creditsocietydb_loans 
                SET Status = 'Closed',
                    PaidAmount = @PaidAmount,
                    ClosingDate = @ClosingDate,
                    TotalInterest = @TotalInterest,
                    TotalPayable = @TotalPayable,
                    IsClosed = TRUE
                WHERE LoanID = @Id";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PaidAmount", model.PaidAmount);
            cmd.Parameters.AddWithValue("@ClosingDate", model.ClosingDate ?? DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@TotalInterest", model.TotalInterest);
            cmd.Parameters.AddWithValue("@TotalPayable", model.TotalPayable);
            cmd.Parameters.AddWithValue("@Id", id);
            
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                return Ok(new { message = "Loan closed successfully" });
            }
            
            return BadRequest(new { message = "Failed to close loan" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpPut("users/{id}/password")]
    public IActionResult UpdatePassword(int id, [FromBody] PasswordModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string checkQuery = "SELECT Role FROM creditsocietydb_users WHERE Id = @Id";
            using var checkCmd = new MySqlCommand(checkQuery, conn);
            checkCmd.Parameters.AddWithValue("@Id", id);
            var role = checkCmd.ExecuteScalar()?.ToString();
            
            if (role == null)
                return NotFound(new { message = "User not found" });
            
            string query = "UPDATE creditsocietydb_users SET Password = @Password WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Password", model.NewPassword);
            cmd.Parameters.AddWithValue("@Id", id);
            
            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                return Ok(new { message = "Password updated successfully" });
            }
            
            return BadRequest(new { message = "Failed to update password" });
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
    public string StartDate { get; set; } = "";
}

public class CloseLoanModel
{
    public decimal PaidAmount { get; set; }
    public string? ClosingDate { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal TotalPayable { get; set; }
}

public class PasswordModel
{
    public string NewPassword { get; set; } = "";
}