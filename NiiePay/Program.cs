using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using NiiePay.Entities;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500") // Chỉ định Port của Frontend
                                .AllowAnyHeader()                     // Cho phép mọi header (Content-Type, Authorization...)
                                .AllowAnyMethod()                     // Cho phép mọi HTTP method (GET, POST, PUT, DELETE, OPTIONS)
                                .AllowCredentials();                  // Bắt buộc nếu có dùng Cookie hoặc Token Authentication
                      });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<NiiePayContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MyEstore")));

var app = builder.Build();

if (app.Environment.IsDevelopment())

{

    app.UseSwagger();

    app.UseSwaggerUI();

}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
