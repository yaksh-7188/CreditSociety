using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using CreditSociety;

namespace CreditSociety.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MemberController : ControllerBase
{
    [HttpGet("member/{id}")]
    public IActionResult GetMemberDetails(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = "SELECT * FROM creditsocietydb_users WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return Ok(new
                {
                    id = reader["Id"],
                    username = reader["Username"],
                    fullName = reader["FullName"],
                    email = reader["Email"],
                    phone = reader["Phone"],
                    role = reader["Role"],
                    joinDate = reader["JoinDate"]
                });
            }
            
            return NotFound(new { message = "Member not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpGet("member/{id}/loans")]
    public IActionResult GetMemberLoans(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                SELECT 
                    LoanID,
                    LoanAmount,
                    InterestRate,
                    LoanTerm,
                    StartDate,
                    EndDate,
                    Status,
                    PaidAmount,
                    ClosingDate,
                    TotalInterest,
                    TotalPayable,
                    IsClosed
                FROM creditsocietydb_loans 
                WHERE UserID = @UserId
                ORDER BY StartDate DESC";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", id);
            using var reader = cmd.ExecuteReader();
            
            var loans = new List<object>();
            while (reader.Read())
            {
                loans.Add(new
                {
                    loanId = reader["LoanID"],
                    amount = Convert.ToDecimal(reader["LoanAmount"]),
                    interestRate = Convert.ToDecimal(reader["InterestRate"]),
                    term = Convert.ToInt32(reader["LoanTerm"]),
                    startDate = reader["StartDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartDate"]).ToString("dd/MM/yyyy"),
                    endDate = reader["EndDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["EndDate"]).ToString("dd/MM/yyyy"),
                    status = reader["Status"]?.ToString() ?? "Active",
                    paidAmount = reader["PaidAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PaidAmount"]),
                    closingDate = reader["ClosingDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ClosingDate"]).ToString("dd/MM/yyyy"),
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

    [HttpGet("member/{id}/emi")]
    public IActionResult GetMemberEMI(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string query = @"
                SELECT * FROM monthly_emi 
                WHERE member_id = @UserId
                ORDER BY month_year DESC";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", id);
            using var reader = cmd.ExecuteReader();
            
            var emiList = new List<object>();
            while (reader.Read())
            {
                emiList.Add(new
                {
                    id = reader["id"],
                    monthYear = Convert.ToDateTime(reader["month_year"]).ToString("MMMM yyyy"),
                    amount = Convert.ToDecimal(reader["amount"]),
                    paidAmount = Convert.ToDecimal(reader["paid_amount"]),
                    paymentDate = reader["payment_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["payment_date"]).ToString("dd/MM/yyyy"),
                    paymentMode = reader["payment_mode"]?.ToString(),
                    status = reader["status"]?.ToString(),
                    lateFee = Convert.ToDecimal(reader["late_fee"])
                });
            }
            
            return Ok(emiList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpGet("member/{id}/stats")]
    public IActionResult GetMemberStats(int id)
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string emiQuery = @"
                SELECT 
                    COUNT(*) as totalEMICount,
                    COALESCE(SUM(amount), 0) as totalEMI,
                    COALESCE(SUM(paid_amount), 0) as totalPaid,
                    COALESCE(SUM(late_fee), 0) as totalLateFee
                FROM monthly_emi 
                WHERE member_id = @UserId";
            
            using var emiCmd = new MySqlCommand(emiQuery, conn);
            emiCmd.Parameters.AddWithValue("@UserId", id);
            using var emiReader = emiCmd.ExecuteReader();
            
            decimal totalEMI = 0;
            decimal totalPaid = 0;
            int emiCount = 0;
            decimal lateFee = 0;
            
            if (emiReader.Read())
            {
                totalEMI = Convert.ToDecimal(emiReader["totalEMI"]);
                totalPaid = Convert.ToDecimal(emiReader["totalPaid"]);
                emiCount = Convert.ToInt32(emiReader["totalEMICount"]);
                lateFee = Convert.ToDecimal(emiReader["totalLateFee"]);
            }
            emiReader.Close();
            
            // ✅ FIX: TotalInterest aur ActiveLoans bhi fetch karo
            string loanQuery = @"
                SELECT 
                    COUNT(*) as totalLoans,
                    COALESCE(SUM(LoanAmount), 0) as totalLoanAmount,
                    COALESCE(SUM(PaidAmount), 0) as totalLoanPaid,
                    COALESCE(SUM(TotalPayable), 0) as totalLoanPayable,
                    COALESCE(SUM(TotalInterest), 0) as totalInterestSum,
                    COUNT(CASE WHEN Status = 'Active' THEN 1 END) as activeLoans
                FROM creditsocietydb_loans 
                WHERE UserID = @UserId";
            
            using var loanCmd = new MySqlCommand(loanQuery, conn);
            loanCmd.Parameters.AddWithValue("@UserId", id);
            using var loanReader = loanCmd.ExecuteReader();
            
            int totalLoans = 0;
            decimal totalLoanAmount = 0;
            decimal totalLoanPaid = 0;
            decimal totalLoanPayable = 0;
            decimal totalInterestSum = 0;
            int activeLoans = 0;

            if (loanReader.Read())
            {
                totalLoans = Convert.ToInt32(loanReader["totalLoans"]);
                totalLoanAmount = Convert.ToDecimal(loanReader["totalLoanAmount"]);
                totalLoanPaid = Convert.ToDecimal(loanReader["totalLoanPaid"]);
                totalLoanPayable = Convert.ToDecimal(loanReader["totalLoanPayable"]);
                totalInterestSum = Convert.ToDecimal(loanReader["totalInterestSum"]);
                activeLoans = Convert.ToInt32(loanReader["activeLoans"]);
            }
            loanReader.Close();
            
            // ✅ FIX: DB se directly interest lo - kabhi negative nahi hoga
            decimal totalInterest = totalInterestSum > 0 ? totalInterestSum : 0;
            
            decimal remainingLoan = totalLoanPayable - totalLoanPaid;
            if (remainingLoan < 0) remainingLoan = 0;
            
            return Ok(new
            {
                totalEMI,
                totalPaid,
                remainingEMI = totalEMI - totalPaid,
                emiCount,
                lateFee,
                totalLoans,
                activeLoans,
                totalLoanAmount,
                totalLoanPaid,
                totalLoanPayable,
                totalInterest,
                remainingLoan
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            
            string memberQuery = "SELECT COUNT(*) FROM creditsocietydb_users WHERE Role = 'Member'";
            using var memberCmd = new MySqlCommand(memberQuery, conn);
            int totalMembers = Convert.ToInt32(memberCmd.ExecuteScalar());
            
            string loanQuery = "SELECT COUNT(*) FROM creditsocietydb_loans WHERE Status = 'Active'";
            using var loanCmd = new MySqlCommand(loanQuery, conn);
            int totalLoans = Convert.ToInt32(loanCmd.ExecuteScalar());
            
            string emiQuery = "SELECT COUNT(*) FROM monthly_emi";
            using var emiCmd = new MySqlCommand(emiQuery, conn);
            int totalEMI = Convert.ToInt32(emiCmd.ExecuteScalar());
            
            string collectionQuery = @"
                SELECT COALESCE(SUM(paid_amount), 0) 
                FROM monthly_emi 
                WHERE month_year = DATE_FORMAT(NOW(), '%Y-%m-01')";
            using var collectionCmd = new MySqlCommand(collectionQuery, conn);
            decimal monthlyCollection = Convert.ToDecimal(collectionCmd.ExecuteScalar());
            
            return Ok(new
            {
                totalMembers,
                totalLoans,
                totalEMI,
                monthlyTotal = monthlyCollection
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }
}