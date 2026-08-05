using Aspire.Hosting.ModularAppHosts;
using PreviewProducer.ModuleContract;

var builder = DistributedApplication.CreateBuilder(args);

var previewManifest = builder.Configuration["ModulePreview:Manifest"]
    ?? throw new InvalidOperationException("ModulePreview:Manifest is required.");
await builder.ApplyModulePreviewManifestAsync(previewManifest);
await PreviewProducerModule.ImportModuleAsync(builder);
await builder.Build().RunAsync();
