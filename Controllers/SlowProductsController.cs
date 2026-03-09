using Microsoft.AspNetCore.Mvc;
using SREPerfDemo.Utilities;

namespace SREPerfDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SlowProductsController : ControllerBase
{
    private readonly ILogger<SlowProductsController> _logger;
    private static readonly List<Product> Products = GenerateProducts();

    public SlowProductsController(ILogger<SlowProductsController> logger)
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
            .ThenBy(p => p.Price)
            .Take(20)
            .ToList();

        return Ok(sortedProducts);
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = 60)]
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
    [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "query" })]
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

    [HttpGet("memory-leak")]
    public async Task<ActionResult<string>> MemoryLeakEndpoint()
    {
        _logger.LogInformation("Triggering memory leak simulation");

        // Simulate memory leak by creating large objects that aren't properly disposed
        var largeList = new List<byte[]>();

        await Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                // Create 1MB byte arrays
                largeList.Add(new byte[1024 * 1024]);
            }
        });

        // Don't dispose or clear the list - simulating a memory leak
        // In a real scenario, this might be stored in a static field
        StaticMemoryHolder.AddToMemory(largeList);

        return Ok($"Added {largeList.Count} MB to memory. Total static memory: {StaticMemoryHolder.GetMemoryCount()} MB");
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