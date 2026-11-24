using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<StoreDb>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default") ??
      "Host=localhost;Database=storedb;Username=storeuser;Password=storepass"));
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDb>();
    await db.Database.EnsureCreatedAsync();
    // Keep existing prototype databases compatible when the delivery-city field is introduced.
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"City\" text NOT NULL DEFAULT ''");
    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product { Name="Everyday Backpack", Description="A practical everyday carry bag.", Price=3490, Stock=18, Category="Accessories", ImageUrl="https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=900" },
            new Product { Name="Minimal Watch", Description="Clean design for everyday wear.", Price=5990, Stock=12, Category="Accessories", ImageUrl="https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=900" },
            new Product { Name="Wireless Headphones", Description="Comfortable wireless listening.", Price=7990, Stock=9, Category="Electronics", ImageUrl="https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=900" },
            new Product { Name="Classic Sneakers", Description="Versatile everyday sneakers.", Price=6490, Stock=15, Category="Fashion", ImageUrl="https://images.unsplash.com/photo-1549298916-b41d501d3772?w=900" }
        );
        await db.SaveChangesAsync();
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/products", async (StoreDb db, string? search, string? category) =>
{
    var q = db.Products.AsNoTracking().Where(p => p.IsActive);
    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(p => p.Name.ToLower().Contains(search.ToLower()));
    if (!string.IsNullOrWhiteSpace(category) && category != "All")
        q = q.Where(p => p.Category == category);
    return Results.Ok(await q.OrderBy(p => p.Name).ToListAsync());
});

app.MapGet("/api/categories", async (StoreDb db) =>
    Results.Ok(await db.Products.Where(p => p.IsActive).Select(p => p.Category).Distinct().OrderBy(x => x).ToListAsync()));

app.MapPost("/api/orders", async ([FromBody] CreateOrder request, StoreDb db) =>
{
    if (request.Items.Count == 0 || string.IsNullOrWhiteSpace(request.CustomerName) ||
        string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Address) || string.IsNullOrWhiteSpace(request.City))
        return Results.BadRequest(new { message = "Name, phone, address and at least one item are required." });

    await using var tx = await db.Database.BeginTransactionAsync();
    var order = new Order {
        CustomerName = request.CustomerName.Trim(),
        Phone = request.Phone.Trim(),
        Address = request.Address.Trim(),
        City = request.City.Trim(),
        PaymentMethod = "Cash on Delivery",
        Status = "Pending"
    };
    foreach (var item in request.Items)
    {
        if (item.Quantity < 1) return Results.BadRequest(new { message = "Invalid quantity." });
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive);
        if (product is null) return Results.BadRequest(new { message = "A selected product is unavailable." });
        if (product.Stock < item.Quantity) return Results.BadRequest(new { message = $"Insufficient stock for {product.Name}." });
        product.Stock -= item.Quantity;
        order.Items.Add(new OrderItem { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = item.Quantity });
    }
    order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    await tx.CommitAsync();
    return Results.Created($"/api/orders/{order.Id}", new { order.Id, order.Total, order.Status });
});

app.MapGet("/api/admin/orders", async (StoreDb db, HttpContext ctx) =>
{
    if (!IsAdmin(ctx, app.Configuration)) return Results.Unauthorized();
    return Results.Ok(await db.Orders.Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ToListAsync());
});

app.MapPatch("/api/admin/orders/{id:int}/status", async (int id, UpdateStatus request, StoreDb db, HttpContext ctx) =>
{
    if (!IsAdmin(ctx, app.Configuration)) return Results.Unauthorized();
    var allowed = new[] { "Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled" };
    if (!allowed.Contains(request.Status)) return Results.BadRequest(new { message = "Invalid status." });
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();
    order.Status = request.Status;
    await db.SaveChangesAsync();
    return Results.Ok(order);
});

app.MapPost("/api/admin/products", async (ProductInput input, StoreDb db, HttpContext ctx) =>
{
    if (!IsAdmin(ctx, app.Configuration)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(input.Name) || input.Price <= 0 || input.Stock < 0)
        return Results.BadRequest(new { message = "Name, positive price and non-negative stock are required." });
    var p = new Product { Name=input.Name.Trim(), Description=input.Description ?? "", Category=input.Category ?? "General",
        Price=input.Price, Stock=input.Stock, ImageUrl=input.ImageUrl ?? "", IsActive=true };
    db.Products.Add(p); await db.SaveChangesAsync();
    return Results.Created($"/api/products/{p.Id}", p);
});

app.MapPut("/api/admin/products/{id:int}", async (int id, ProductInput input, StoreDb db, HttpContext ctx) =>
{
    if (!IsAdmin(ctx, app.Configuration)) return Results.Unauthorized();
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(input.Name) || input.Price <= 0 || input.Stock < 0)
        return Results.BadRequest(new { message = "Invalid product data." });
    p.Name=input.Name.Trim(); p.Description=input.Description ?? ""; p.Category=input.Category ?? "General";
    p.Price=input.Price; p.Stock=input.Stock; p.ImageUrl=input.ImageUrl ?? "";
    await db.SaveChangesAsync(); return Results.Ok(p);
});

app.MapDelete("/api/admin/products/{id:int}", async (int id, StoreDb db, HttpContext ctx) =>
{
    if (!IsAdmin(ctx, app.Configuration)) return Results.Unauthorized();
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();
    p.IsActive = false; await db.SaveChangesAsync(); return Results.NoContent();
});

app.Run();

static bool IsAdmin(HttpContext ctx, IConfiguration config)
{
    var expected = config["Admin:ApiKey"] ?? "";
    var supplied = ctx.Request.Headers["X-Admin-Key"].ToString();
    if (expected.Length < 24 || supplied.Length != expected.Length) return false;
    return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied));
}

class StoreDb(DbContextOptions<StoreDb> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>().Property(x => x.Price).HasPrecision(12,2);
        b.Entity<Order>().Property(x => x.Total).HasPrecision(12,2);
        b.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(12,2);
        b.Entity<Order>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
class Product { public int Id {get;set;} public string Name {get;set;}=""; public string Description {get;set;}="";
 public string Category {get;set;}="General"; public decimal Price {get;set;} public int Stock {get;set;}
 public string ImageUrl {get;set;}=""; public bool IsActive {get;set;}=true; }
class Order { public int Id {get;set;} public string CustomerName {get;set;}=""; public string Phone {get;set;}="";
 public string Address {get;set;}=""; public string City {get;set;}=""; public string PaymentMethod {get;set;}="Cash on Delivery";
 public string Status {get;set;}="Pending"; public decimal Total {get;set;} public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
 public List<OrderItem> Items {get;set;}=[]; }
class OrderItem { public int Id {get;set;} public int OrderId {get;set;} public int ProductId {get;set;}
 public string ProductName {get;set;}=""; public decimal UnitPrice {get;set;} public int Quantity {get;set;} }
record OrderLine(int ProductId, int Quantity);
record CreateOrder(string CustomerName, string Phone, string Address, string City, List<OrderLine> Items);
record UpdateStatus(string Status);
record ProductInput(string Name, string? Description, string? Category, decimal Price, int Stock, string? ImageUrl);
