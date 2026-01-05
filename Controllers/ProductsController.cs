using EcommerceStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Products?category=Men
    public async Task<IActionResult> Index(string category)
    {
        var products = string.IsNullOrEmpty(category)
            ? await _context.Products.ToListAsync()
            : await _context.Products
                .Where(p => p.Category == category)
                .ToListAsync();

        ViewBag.SelectedCategory = category;
        return View(products);
    }
}
