using CreditSociety;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddCors(p => p.AddPolicy("cors", policy =>
{
    policy.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

// Create tables
try
{
    DatabaseHelper.CreateTables();
    Console.WriteLine("✅ Database connected!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database error: {ex.Message}");
}

app.UseCors("cors");
app.UseAuthorization();
app.MapControllers();

// IMPORTANT - Default file set karo
DefaultFilesOptions options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();
options.DefaultFileNames.Add("login.html");  // ← login.html default page

app.UseDefaultFiles(options);
app.UseStaticFiles();

app.Run();