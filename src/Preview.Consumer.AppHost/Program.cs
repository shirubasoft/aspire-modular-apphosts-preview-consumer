using PreviewProducer.ModuleContract;

var builder = DistributedApplication.CreateBuilder(args);

await PreviewProducerModule.ImportModuleAsync(builder);
await builder.Build().RunAsync();
