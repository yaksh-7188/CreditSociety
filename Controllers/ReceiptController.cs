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
                string mode = reader["payment_mode"] == DBNull.Value ? "N/A" : reader["payment_mode"]?.ToString() ?? "N/A";
                
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
                decimal paid = reader["PaidAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PaidAmount"]);
                decimal totalInterest = reader["TotalInterest"] == DBNull.Value ? (amount * rate / 100) : Convert.ToDecimal(reader["TotalInterest"]);
                decimal totalPayable = reader["TotalPayable"] == DBNull.Value ? (amount + totalInterest) : Convert.ToDecimal(reader["TotalPayable"]);
                string closingDate = reader["ClosingDate"] == DBNull.Value ? "Not Closed" : Convert.ToDateTime(reader["ClosingDate"]).ToString("dd/MM/yyyy");
                
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("═══════════════════════════════════════");
                csv.AppendLine("     CREDIT SOCIETY - LOAN RECEIPT     ");
                csv.AppendLine("═══════════════════════════════════════");
                csv.AppendLine($"Receipt No      : LOAN-{id}");
                csv.AppendLine($"Date           : {DateTime.Now:dd/MM/yyyy}");
                csv.AppendLine("───────────────────────────────────────");
                csv.AppendLine($"Member Name    : {memberName}");
                csv.AppendLine($"Principal      : ₹{amount}");
                csv.AppendLine($"Interest Rate  : {rate}%");
                csv.AppendLine($"Interest Amount: ₹{totalInterest}");
                csv.AppendLine($"Total Payable  : ₹{totalPayable}");
                csv.AppendLine($"Paid Amount    : ₹{paid}");
                csv.AppendLine($"Closing Date   : {closingDate}");
                csv.AppendLine($"Status         : {(reader["IsClosed"] != DBNull.Value && Convert.ToBoolean(reader["IsClosed"]) ? "Closed" : reader["Status"])}");
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