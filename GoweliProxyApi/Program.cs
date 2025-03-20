using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Register the Swagger generator
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure the HTTP client
builder.Services.AddHttpClient("OpenLibraryClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    //CORS headers (only required if app were in production and used by several users)
    //client.DefaultRequestHeaders.Add("User-Agent", "Goweli Book Application/1.0");
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();  // Adds the Swagger JSON endpoint
    app.UseSwaggerUI(); // Adds the Swagger UI
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Use CORS before routing
app.UseCors("AllowAllOrigins");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Fallback to serving the Blazor WebAssembly app
app.MapFallbackToFile("index.html");

app.Run();