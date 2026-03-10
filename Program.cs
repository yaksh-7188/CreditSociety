using CreditSociety;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddCors(p => p.AddPolicy("cors", policy =>
{
    policy.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

try
{
    DatabaseHelper.CreateTables();
    Console.WriteLine("✅ Database connected!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database error: {ex.Message}");
}

DefaultFilesOptions options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();
options.DefaultFileNames.Add("Login.html");
app.UseDefaultFiles(options);
app.UseStaticFiles();

app.UseCors("cors");
app.UseAuthorization();
app.MapControllers();

app.Run();