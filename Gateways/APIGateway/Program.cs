using Consul;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("Routers/ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"Routers/ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services
    .AddOcelot(builder.Configuration)
    .AddConsul()
    .AddPolly();

builder.Services.AddHttpClient(); 

builder.Services.AddSingleton<IConsulClient>(_ =>
    new Consul.ConsulClient(c =>
        c.Address = new Uri("http://consul:8500")));

var app = builder.Build();


app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/api/healths"),
    branch =>
    {
        branch.UseRouting();
        branch.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/api/healths/{service}", async (string service, IConsulClient consul, IHttpClientFactory http) =>
            {
                var res = await consul.Health.Service(service, tag: null, passingOnly: true);
                var inst = res.Response.FirstOrDefault();
                if (inst is null)
                    return Results.NotFound(new { error = $"No healthy instance for '{service}'" });

                var addr = $"{inst.Service.Address}:{inst.Service.Port}";
                var target = new Uri($"http://{addr}/health");

                var client = http.CreateClient();
                using var resp = await client.GetAsync(target);
                var body = await resp.Content.ReadAsStringAsync();

                return Results.Json(new
                {
                    service,
                    address = addr,
                    statusCode = (int)resp.StatusCode,
                    output = body
                }, statusCode: (int)resp.StatusCode);
            });
        });
    });


await app.UseOcelot();

app.Run();
