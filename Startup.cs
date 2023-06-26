using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;


public class Product
{
    public int id { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public decimal price { get; set; }
    public string User { get; set; }
}

public class ExampleDto
{
    public string User { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       
        optionsBuilder.UseMySql("Server=localhost;Port:3306;Database=products;Uid=root;");
    }
        
    }


[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly HttpClient _httpClient;

    public ProductsController(AppDbContext dbContext, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
    }

    [HttpPost]
    public async Task<IActionResult> Post(ExampleDto exampleDto)
    {
        var products = await GetProductsFromApi();
        await SaveProductsToDatabase(products, exampleDto.User);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Get(string user)
    {
        var products = await _dbContext.Products
            .Where(p => p.User == user)
            .ToListAsync();

        return Ok(products);
    }

    private async Task<List<Product>> GetProductsFromApi()
    {
        var response = await _httpClient.GetAsync("https://dummyjson.com/products");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<Product>>();
    }

    private async Task SaveProductsToDatabase(List<Product> products, string user)
    {
        foreach (var product in products)
        {
            product.User = user;
        }

        _dbContext.Products.AddRange(products);
        await _dbContext.SaveChangesAsync();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<AppDbContext>();
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();
        }

        app.MapControllers();
        app.Run();
    }
}
