using BusinessCardManager.data;
using BusinessCardManager.Repositories.Interfaces;
using BusinessCardManager.Repositories.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngularApp", policy =>
            {
                policy.WithOrigins("http://localhost:4200/", "http://127.0.0.1:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // Configure JSON options to handle object cycles
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        // Get the connection string settings 
        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        // Configure database context
        builder.Services.AddDbContext<BusinessCardDbContext>(opt => opt.UseSqlServer(connectionString));

        // Register BusinessCardService with additional parameter
        builder.Services.AddScoped<IBusinessCard>(provider =>
        {
            var context = provider.GetRequiredService<BusinessCardDbContext>();
            var backupDirectory = "C:\\Users\\Student\\Downloads"; // Example of a backup directory or other necessary string parameter
            return new BusinessCardService(context, backupDirectory);
        });

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Enable CORS
        app.UseCors("AllowAngularApp");

        // Configure the HTTP request pipeline.
        // Remove the environment check to always enable Swagger
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "BusinessCardManager API V1");
            c.RoutePrefix = string.Empty; // Makes Swagger available at the root URL
        });

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
