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
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
    public ActionResult<IEnumerable<Product>> GetProducts()
    {
        _logger.LogInformation("Getting all products (slow version)");

        var products = Products
            .OrderBy(p => p.Name)
            .Take(20);

        return Ok(products);
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public ActionResult<Product> GetProduct(int id)
    {
        _logger.LogInformation("Getting product {ProductId} (slow version)", id);

        var product = Products.FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpGet("search")]
    [ResponseCache(Duration = 120, VaryByQueryKeys = new[] { "*" })]
    public ActionResult<IEnumerable<Product>> SearchProducts([FromQuery] string query)
    {
        _logger.LogInformation("Searching products with query: {Query} (slow version)", query);

        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(Products.Take(10));
        }

        var results = Products
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10);

        return Ok(results);
    }

    [HttpGet("memory-leak")]
    public ActionResult<string> MemoryLeakEndpoint()
    {
        _logger.LogInformation("Triggering memory leak simulation");

        var largeList = new List<byte[]>();

        for (int i = 0; i < 100; i++)
        {
            largeList.Add(new byte[1024 * 1024]);
        }

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