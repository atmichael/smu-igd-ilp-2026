using ILP.AppHost.Config;

var builder = DistributedApplication.CreateBuilder(args);

AspireConfig.Initialize(builder.Configuration);

var backend = builder.AddProject<Projects.ILP_Server>("backend")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ALLOWEDHOSTS", "*");

var frontend = builder.AddViteApp("frontend", AspireConfig.FrontendPath)
    .WithReference(backend)
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints();


builder.Build().Run();
