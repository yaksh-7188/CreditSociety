using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using CreditSociety;
using System.Text;

namespace CreditSociety.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReceiptController : ControllerBase
{
    [HttpGet("emi/{id}")]
    public IActionResult DownloadEMIReceipt(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                SELECT m.*, u.FullName, u.Username 
                FROM monthly_emi m
                JOIN creditsocietydb_users u ON m.member_id = u.Id
                WHERE m.id = @Id";
                
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = cmd.ExecuteReader();
            
            if (reader.Read())
            {
                string memberName = reader["FullName"]?.ToString() ?? "Unknown";
                string month = Convert.ToDateTime(reader["month_year"]).ToString("MMMM yyyy");
                decimal amount = Convert.ToDecimal(reader["amount"]);
                decimal paid = Convert.ToDecimal(reader["paid_amount"]);
                string date = reader["payment_date"] == DBNull.Value ? "Not Paid" : Convert.ToDateTime(reader["payment_date"]).ToString("dd/MM/yyyy");
                string mode = reader["payment_mode"] == DBNull.Value ? "N/A" : reader["payment_mode"].ToString();
                
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("═══════════════════════════════════════");
                csv.AppendLine("     CREDIT SOCIETY - EMI RECEIPT      ");
                csv.AppendLine("═══════════════════════════════════════");
                csv.AppendLine($"Receipt No      : EMI-{id}");
                csv.AppendLine($"Date           : {DateTime.Now:dd/MM/yyyy}");
                csv.AppendLine("───────────────────────────────────────");
                csv.AppendLine($"Member Name    : {memberName}");
                csv.AppendLine($"Month          : {month}");
                csv.AppendLine($"Amount         : ₹{amount}");
                csv.AppendLine($"Paid Amount    : ₹{paid}");
                csv.AppendLine($"Payment Date   : {date}");
                csv.AppendLine($"Payment Mode   : {mode}");
                csv.AppendLine($"Status         : {reader["status"]}");
                csv.AppendLine("───────────────────────────────────────");
                csv.AppendLine("Thank you for your payment!");
                csv.AppendLine("═══════════════════════════════════════");
                
                byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"EMI_Receipt_{id}_{DateTime.Now:yyyyMMdd}.csv");
            }
            
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
    
    [HttpGet("loan/{id}")]
    public IActionResult DownloadLoanReceipt(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                SELECT l.*, u.FullName 
                FROM creditsocietydb_loans l
                JOIN creditsocietydb_users u ON l.UserID = u.Id
                WHERE l.LoanID = @Id";
                
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = cmd.ExecuteReader();
            
            if (reader.Read())
            {
                string memberName = reader["FullName"]?.ToString() ?? "Unknown";
                decimal amount = Convert.ToDecimal(reader["LoanAmount"]);
                decimal rate = Convert.ToDecimal(reader["InterestRate"]);
                int term = Convert.ToInt32(reader["LoanTerm"]);
                string start = Convert.ToDateTime(reader["StartDate"]).ToString("dd/MM/yyyy");
                string end = Convert.ToDateTime(reader["EndDate"]).ToString("dd/MM/yyyy");
                
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("═══════════════════════════════════════");
                csv.AppendLine("     CREDIT SOCIETY - LOAN RECEIPT     ");
                csv.AppendLine("═══════════════════════════════════════");
                csv.AppendLine($"Receipt No      : LOAN-{id}");
                csv.AppendLine($"Date           : {DateTime.Now:dd/MM/yyyy}");
                csv.AppendLine("───────────────────────────────────────");
                csv.AppendLine($"Member Name    : {memberName}");
                csv.AppendLine($"Loan Amount    : ₹{amount}");
                csv.AppendLine($"Interest Rate  : {rate}%");
                csv.AppendLine($"Term           : {term} months");
                csv.AppendLine($"Start Date     : {start}");
                csv.AppendLine($"End Date       : {end}");
                csv.AppendLine($"Status         : {reader["Status"]}");
                csv.AppendLine("───────────────────────────────────────");
                csv.AppendLine("Thank you for choosing Credit Society!");
                csv.AppendLine("═══════════════════════════════════════");
                
                byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"Loan_Receipt_{id}_{DateTime.Now:yyyyMMdd}.csv");
            }
            
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}