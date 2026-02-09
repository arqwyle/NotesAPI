using Microsoft.EntityFrameworkCore;
using NotesAPI.Database;
using NotesAPI.Repositories;
using NotesAPI.Repositories.Interfaces;
using NotesAPI.Services;
using NotesAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisConnection = builder.Configuration["CacheSettings:RedisConnectionString"] 
                          ?? "localhost:6379";
    options.Configuration = redisConnection;
    options.InstanceName = "NotesAPI_";
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

var app = builder.Build();

app.UseExceptionHandler("/error");

app.Map("/error", (HttpContext context) =>
{
    context.Response.StatusCode = 500;
    return Results.Json(new { error = "An unexpected error occurred." });
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();