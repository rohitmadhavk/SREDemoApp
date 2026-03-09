using Microsoft.AspNetCore.Mvc;
using SREPerfDemo.Utilities;

namespace SREPerfDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CpuIntensiveController : ControllerBase
{
    private readonly ILogger<CpuIntensiveController> _logger;
    private static readonly List<Product> Products = GenerateProducts();

    public CpuIntensiveController(ILogger<CpuIntensiveController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 60)]
    public ActionResult<IEnumerable<Product>> GetProducts()
    {
        _logger.LogInformation("Getting all products");

        var sortedProducts = Products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Category)
            .Take(20)
            .ToList();

        return Ok(sortedProducts);
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = 300)]
    public ActionResult<Product> GetProduct(int id)
    {
        _logger.LogInformation("Getting product {ProductId}", id);

        var product = Products.FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpGet("search")]
    [ResponseCache(Duration = 120, VaryByQueryKeys = new[] { "query" })]
    public ActionResult<IEnumerable<Product>> SearchProducts([FromQuery] string query)
    {
        _logger.LogInformation("Searching products with query: {Query}", query);

        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(Products.Take(10));
        }

        var results = Products
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        return Ok(results);
    }

    [HttpGet("cpu-stress")]
    public ActionResult<string> CpuStressTest()
    {
        _logger.LogInformation("CPU stress test endpoint");
        return Ok("CPU stress test endpoint is disabled in production.");
    }

    [HttpGet("memory-cpu-leak")]
    public ActionResult<string> MemoryCpuLeak()
    {
        _logger.LogInformation("Memory/CPU leak simulation endpoint");
        return Ok($"Total static memory: {StaticMemoryHolder.GetMemoryCount()} MB");
    }

    private static List<Product> GenerateProducts()
    {
        var categories = new[] { "Electronics", "Clothing", "Books", "Home", "Sports", "Food" };
        var products = new List<Product>();

        for (int i = 1; i <= 1000; i++)
        {
            products.Add(new Product
            {
                Id = i,
                Name = $"Product {i}",
                Category = categories[Random.Shared.Next(categories.Length)],
                Price = Math.Round(Random.Shared.NextDouble() * 1000, 2),
                InStock = Random.Shared.Next(0, 100) > 20
            });
        }

        return products;
    }
}
