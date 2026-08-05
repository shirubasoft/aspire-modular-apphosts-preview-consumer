using Aspire.Hosting.ModularAppHosts;
using PreviewProducer.ModuleContract;

var builder = DistributedApplication.CreateBuilder(args);

var previewResolution = builder.Configuration["ModulePreview:Resolution"]
    ?? throw new InvalidOperationException("ModulePreview:Resolution is required.");
await builder.ApplyModulePreviewResolutionAsync(previewResolution);
await PreviewProducerModule.ImportModuleAsync(builder);
await builder.Build().RunAsync();
