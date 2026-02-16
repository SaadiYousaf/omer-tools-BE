using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Business.Mappings;
using ProductService.Business.Services;
using ProductService.DataAccess.Data;
using ProductService.DataAccess.Repositories;
using ProductService.Domain.Entites.EmailsModel;
using ProductService.Domain.Interfaces;
using SendGrid;
using src.ProductService.DataAccess;
using Stripe;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configure CORS - ADD LOCALHOST FOR DEVELOPMENT
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowReactApp", policy =>
//    {
//        policy.WithOrigins("http://localhost:3000", "https://omertool.pages.dev", "https://omertools.com.au", "https://omertoolsadmin.pages.dev")
//              .AllowAnyMethod()
//              .AllowAnyHeader()
//              .AllowCredentials();
//    });


//});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
             .AllowAnyMethod()  // Allow any HTTP method (GET, POST, PUT, DELETE, etc.)
              .AllowAnyHeader()  // Allow any headers
              .SetPreflightMaxAge(TimeSpan.FromHours(1))
              .WithExposedHeaders("*");
        //.AllowCredentials(); // Allow credentials if needed
    });
});
// Database Configuration
builder.Services.AddDbContext<ProductDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(120),
                errorNumbersToAdd: null);
             sqlServerOptions.MigrationsAssembly("ProductService.DataAccess");
        });
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Stripe Configuration
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Register Stripe services
builder.Services.AddSingleton<Stripe.PaymentIntentService>();
builder.Services.AddSingleton<Stripe.PaymentMethodService>();
builder.Services.AddSingleton<Stripe.RefundService>();

// Payment Processor
builder.Services.AddScoped<IPaymentProcessor, StripePaymentProcessor>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ProductService.Business.DTOs.IPasswordHasher, PasswordHasher>();

// JWT Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Email services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<ISendGridClient>(sp =>
    new SendGridClient(builder.Configuration["SendGrid:ApiKey"]));
builder.Services.Configure<EmailSettings>(
	builder.Configuration.GetSection("MailkitEmailSettings"));
builder.Services.AddSingleton<IMailkitEmailService,MailkitEmailService>();

// Configure AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
    cfg.DisableConstructorMapping();
    cfg.ShouldUseConstructor = constructor => constructor.IsPublic;
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var validAudiences = jwtSettings.GetSection("Audience").Get<string[]>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudiences = validAudiences,
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Secret"]))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy =>
         policy.RequireRole("Admin", "SuperAdmin"));
});

// Additional repositories and services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
builder.Services.AddScoped<IProductService, ProductService.Business.Services.ProductService>();
builder.Services.AddScoped<ISEOService, SEOService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IWarrantyService, WarrantyService>();
builder.Services.AddScoped<IWarrantyService>(provider =>
	new WarrantyService(
		provider.GetRequiredService<IUnitOfWork>(),
		provider.GetRequiredService<IMapper>(),
		provider.GetRequiredService<ILogger<WarrantyService>>(),
		provider.GetRequiredService<ProductDbContext>(),
		provider.GetRequiredService<IMailkitEmailService>()  // ← ADD THIS
	)
);


// Register services

builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IUserPaymentMethodService, UserPaymentMethodService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
// Register core dependencies
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductService, ProductService.Business.Services.ProductService>();
builder.Services.AddScoped<ProductService.Business.Interfaces.IUserService, ProductService.Business.Services.UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// SIMPLIFIED MIDDLEWARE PIPELINE
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapMethods("/{*path}", new[] { "OPTIONS" }, async (HttpContext context) =>
{
    context.Response.StatusCode = 200;
    await context.Response.WriteAsync("OK");
});


// Stripe Webhook Endpoint
app.MapPost("/webhooks/stripe", async (HttpContext context) =>
{
    try
    {
        var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var stripeSignature = context.Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(stripeSignature))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Missing Stripe-Signature header");
            return;
        }

        var webhookSecret = app.Configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret))
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Webhook secret not configured");
            return;
        }

        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogInformation($"Received Stripe event: {stripeEvent.Type}");

        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Webhook processed successfully");
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Webhook processing error");
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Webhook error");
    }
});

app.MapControllers();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ProductDbContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Migration error");
    }
}

app.Run();