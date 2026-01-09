using Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenAccessor, CookieTokenAccessor>();

var apiBase = builder.Configuration["NotificationApi:BaseUrl"];
builder.Services.AddHttpClient<NotificationApiClient>(http =>
{
    http.BaseAddress = new Uri(apiBase!);
});

// CORS không cần ở web client (server-side), chỉ cần ở API. 
// Nếu bạn phục vụ FE khác origin từ đây thì thêm CORS tương ứng.

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ✅ BẮT BUỘC
app.UseRouting();

app.MapRazorPages();

app.Run();
