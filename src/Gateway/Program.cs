using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(async transformContext =>
        {
            transformContext.ProxyRequest.Headers.Add("CorrelationId", Guid.NewGuid().ToString());
            await Task.CompletedTask;
        });
    });

var app = builder.Build();

app.MapReverseProxy();

app.Run();