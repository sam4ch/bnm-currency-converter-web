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

// Регистрация HTTP-клиента BNM с таймаутом и конфигурацией URL
builder.Services.AddHttpClient(nameof(BnmRateClient), client =>
{
    client.Timeout = BnmRateClient.DefaultTimeout;
});

builder.Services.AddTransient<IBnmRateClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(BnmRateClient));
    var clock = sp.GetRequiredService<IClock>();
    var config = sp.GetRequiredService<IConfiguration>();
    string baseUrl = config.GetValue<string>("Bnm:BaseUrl") ?? BnmRateClient.DefaultBaseUrl;
    return new BnmRateClient(httpClient, clock, baseUrl);
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
