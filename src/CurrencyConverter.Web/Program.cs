using CurrencyConverter.Core.Calculation;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Validation;
using CurrencyConverter.Infrastructure.Bnm;
using CurrencyConverter.Infrastructure.Caching;
using CurrencyConverter.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

// Добавление Razor Pages
builder.Services.AddRazorPages();

// Регистрация сервисов приложения (DI)
builder.Services.AddSingleton<IClock, ChisinauClock>();
builder.Services.AddSingleton<ICurrencyCalculator, CurrencyCalculator>();
builder.Services.AddSingleton<IValidationService, RequestValidator>();

// Регистрация HTTP-клиента BNM с таймаутом
builder.Services.AddHttpClient<IBnmRateClient, BnmRateClient>(client =>
{
    client.Timeout = BnmRateClient.DefaultTimeout;
});

// Регистрация постоянного файлового JSON-кэша в App_Data
builder.Services.AddSingleton<ICacheStore>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var config = sp.GetRequiredService<IConfiguration>();

    string relativeOrAbsolutePath = config.GetValue<string>("Cache:FilePath") ?? "App_Data/rates_cache.json";
    string fullPath = Path.IsPathRooted(relativeOrAbsolutePath)
        ? relativeOrAbsolutePath
        : Path.Combine(env.ContentRootPath, relativeOrAbsolutePath);

    return new JsonCacheStore(fullPath);
});

builder.Services.AddTransient<IRateLookupService, RateLookupService>();
builder.Services.AddTransient<IConversionService, ConversionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.MapRazorPages();

app.Run();
