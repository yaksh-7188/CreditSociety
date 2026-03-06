using MySql.Data.MySqlClient;

namespace CreditSociety;

public static class DatabaseHelper
{
    // ⚠️ Tamaru MySQL password ahiya set karo
    private static string connectionString = "Server=localhost;Database=creditsocietydb;UserId=root;Password=Yaksh@7188;";

    public static MySqlConnection GetConnection()
    {
        return new MySqlConnection(connectionString);
    }

    public static void CreateTables()
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            Console.WriteLine("✅ Connected to creditsocietydb database!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database connection failed: {ex.Message}");
        }
    }
}