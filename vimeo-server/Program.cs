using vimeo_server.Endpoints;
using vimeo_server.Services;
using tusdotnet;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TusFileService>();

var app = builder.Build();

// Create videos directory if it doesn't exist
var videosPath = Path.Combine(app.Environment.ContentRootPath, "videos");
if (!Directory.Exists(videosPath))
{
    Directory.CreateDirectory(videosPath);
}

// Configure TUS middleware first
var tusFileService = app.Services.GetRequiredService<TusFileService>();
app.UseTus(httpContext => tusFileService.GetTusConfiguration());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map video endpoints last
app.MapVideoEndpoints();

await app.RunAsync();