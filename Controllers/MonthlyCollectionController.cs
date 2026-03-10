using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using CreditSociety;

namespace CreditSociety.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MonthlyCollectionController : ControllerBase
{
    [HttpPost("generate")]
    public IActionResult GenerateMonthlyEMI([FromBody] MonthYearModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            var members = new List<int>();
            string memberQuery = "SELECT Id FROM creditsocietydb_users WHERE Role = 'Member'";
            using (var memberCmd = new MySqlCommand(memberQuery, conn))
            using (var reader = memberCmd.ExecuteReader())
            {
                while (reader.Read())
                    members.Add(reader.GetInt32("Id"));
            }

            foreach (var memberId in members)
            {
                string checkQuery = "SELECT COUNT(*) FROM monthly_emi WHERE member_id = @MemberId AND month_year = @MonthYear";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@MemberId", memberId);
                    checkCmd.Parameters.AddWithValue("@MonthYear", model.MonthYear);
                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    
                    if (count == 0)
                    {
                        string insertQuery = @"
                            INSERT INTO monthly_emi (member_id, month_year, amount, status)
                            VALUES (@MemberId, @MonthYear, 1000, 'Pending')";
                        using var insertCmd = new MySqlCommand(insertQuery, conn);
                        insertCmd.Parameters.AddWithValue("@MemberId", memberId);
                        insertCmd.Parameters.AddWithValue("@MonthYear", model.MonthYear);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }

            return Ok(new { message = "Monthly EMI generated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpGet("{monthYear}")]
    public IActionResult GetMonthlyEMI(string monthYear)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            string query = @"
                SELECT m.*, u.FullName as member_name 
                FROM monthly_emi m
                JOIN creditsocietydb_users u ON m.member_id = u.Id
                WHERE m.month_year = @MonthYear
                ORDER BY u.FullName";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MonthYear", monthYear);
            using var reader = cmd.ExecuteReader();

            var list = new List<object>();
            while (reader.Read())
            {
                list.Add(new
                {
                    id = reader["id"],
                    member_id = reader["member_id"],
                    member_name = reader["member_name"]?.ToString() ?? "",
                    amount = Convert.ToDecimal(reader["amount"]),
                    paid_amount = Convert.ToDecimal(reader["paid_amount"]),
                    payment_date = reader["payment_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["payment_date"]).ToString("yyyy-MM-dd"),
                    payment_mode = reader["payment_mode"]?.ToString() ?? "",
                    status = reader["status"]?.ToString() ?? "Pending",
                    late_fee = Convert.ToDecimal(reader["late_fee"])
                });
            }

            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpGet("all")]
    public IActionResult GetAllMonthlyEMI()
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            string query = @"
                SELECT m.*, u.FullName as member_name 
                FROM monthly_emi m
                JOIN creditsocietydb_users u ON m.member_id = u.Id
                ORDER BY m.month_year DESC, u.FullName";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<object>();
            while (reader.Read())
            {
                list.Add(new
                {
                    id = reader["id"],
                    member_id = reader["member_id"],
                    member_name = reader["member_name"]?.ToString() ?? "",
                    amount = Convert.ToDecimal(reader["amount"]),
                    paid_amount = Convert.ToDecimal(reader["paid_amount"]),
                    payment_date = reader["payment_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["payment_date"]).ToString("yyyy-MM-dd"),
                    payment_mode = reader["payment_mode"]?.ToString() ?? "",
                    status = reader["status"]?.ToString() ?? "Pending",
                    late_fee = Convert.ToDecimal(reader["late_fee"])
                });
            }

            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpPut("update/{id}")]
    public IActionResult UpdateMonthlyEMI(int id, [FromBody] UpdateEMIModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            string status = "Pending";
            if (model.PaidAmount >= 1000)
                status = "Paid";
            else if (model.PaidAmount > 0)
                status = "Partial";

            string query = @"
                UPDATE monthly_emi 
                SET paid_amount = @PaidAmount,
                    payment_date = @PaymentDate,
                    payment_mode = @PaymentMode,
                    late_fee = @LateFee,
                    status = @Status
                WHERE id = @Id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PaidAmount", model.PaidAmount);
            cmd.Parameters.AddWithValue("@PaymentDate", model.PaymentDate ?? DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@PaymentMode", model.PaymentMode ?? "Cash");
            cmd.Parameters.AddWithValue("@LateFee", model.LateFee);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Id", id);

            cmd.ExecuteNonQuery();
            return Ok(new { message = "Updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpPost("mark-all-paid")]
    public IActionResult MarkAllPaid([FromBody] BulkPaymentModel model)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            string query = @"
                UPDATE monthly_emi 
                SET paid_amount = 1000,
                    payment_date = @PaymentDate,
                    payment_mode = @PaymentMode,
                    late_fee = 0,
                    status = 'Paid'
                WHERE month_year = @MonthYear";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MonthYear", model.MonthYear ?? "");
            cmd.Parameters.AddWithValue("@PaymentDate", model.PaymentDate ?? DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@PaymentMode", model.PaymentMode ?? "Cash");

            cmd.ExecuteNonQuery();
            return Ok(new { message = "All members marked as paid" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }
}

public class MonthYearModel
{
    public string? MonthYear { get; set; }
}

public class UpdateEMIModel
{
    public decimal PaidAmount { get; set; }
    public string? PaymentDate { get; set; }
    public string? PaymentMode { get; set; }
    public decimal LateFee { get; set; }
}

public class BulkPaymentModel
{
    public string? MonthYear { get; set; }
    public string? PaymentMode { get; set; }
    public string? PaymentDate { get; set; }
}