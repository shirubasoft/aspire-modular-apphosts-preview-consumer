# Aspire Modular AppHosts preview consumer

This repository is the trusted E2E consumer (Repo D) for the cross-repository module preview
sample. Its `main`-branch workflow validates an immutable preview manifest, checks out only the
allowlisted producer at its exact commit, packs that commit's module contract into an ephemeral
local NuGet feed, and starts the consumer AppHost against the selected producer source.

The fixture demonstrates three independent pins:

- The trusted workflow owns an exact `aspire-modular-apphosts` commit, packs it at a unique version,
  and direct-references that package so the run exercises `ApplyModulePreviewManifestAsync` itself.
- `Shirubasoft.Aspire.ModularAppHosts.PreviewProducer.Contract` is packed at a unique, exact
  per-run preview version from the validated producer commit.
- The manifest selects the producer source with its immutable commit.

The AppHost receives only the validated manifest path and applies it before module import:

```csharp
await builder.ApplyModulePreviewManifestAsync(previewManifest);
await PreviewProducerModule.ImportModuleAsync(builder);
```

No legacy repository or revision environment-variable override is present, so the E2E cannot pass
without the manifest API selecting the producer checkout.

The workflow currently pins library commit
[`d770337a432eb29a4dc827ad19c59f1eba9e1a9c`](https://github.com/shirubasoft/aspire-modular-apphosts/commit/d770337a432eb29a4dc827ad19c59f1eba9e1a9c).

## Trigger the E2E workflow

The required `manifest_json` input uses the preview tool's canonical schema. The fixture accepts
only `https://github.com/shirubasoft/aspire-modular-apphosts-preview-producer.git`, module
`preview-producer`, a clean producer snapshot, and a full 40- or 64-hex commit ID.
The module commit must equal the producer commit.

The producer feature branch currently has this example manifest:

```json
{
  "schemaVersion": 1,
  "producer": {
    "repository": "https://github.com/shirubasoft/aspire-modular-apphosts-preview-producer.git",
    "commit": "df0260ead86cb6e3848cad6a0782308f2152d76c",
    "branch": "codex/preview-resource-change",
    "baseRef": "refs/heads/main",
    "baseCommit": "4ca008a6c8ecbe22f2c88ca8099f3b14c997b7c8",
    "dirty": false
  },
  "modules": [
    {
      "name": "preview-producer",
      "repository": "https://github.com/shirubasoft/aspire-modular-apphosts-preview-producer.git",
      "commit": "df0260ead86cb6e3848cad6a0782308f2152d76c",
      "branch": "codex/preview-resource-change",
      "baseRef": "refs/heads/main",
      "baseCommit": "4ca008a6c8ecbe22f2c88ca8099f3b14c997b7c8"
    }
  ]
}
```

Save it as `preview-manifest.json`, then use the preview tool to dispatch the trusted workflow from
`main`:

```bash
dotnet modular-apphosts preview trigger \
  --manifest preview-manifest.json \
  --repo shirubasoft/aspire-modular-apphosts-preview-consumer \
  --workflow module-preview-e2e.yml \
  --ref main \
  --input correlation_id=local-verification
```

`callback_metadata_json` is optional opaque JSON. It is retained in diagnostics and provenance but
is never interpreted as a URL or invoked by the workflow.

## What the workflow proves

The workflow:

1. Runs only from the trusted `main` ref with `contents: read` and no repository secrets.
2. Validates the repository, module, clean state, and immutable commit before checkout.
3. Checks out the workflow-owned library commit and validated producer commit with credentials
   persistence disabled.
4. Packs the exact library and producer contract under unique exact versions.
5. Restores the consumer against both packages from the isolated local feed and applies the
   validated manifest before import.
6. Starts only with the pinned Aspire CLI, then waits for both `preview-producer-api` and the
   feature-only `preview-producer-sidecar`.
7. Discovers the API URL through `aspire describe` and requires the exact response
   `preview-from-feature-branch`.
8. Always stops the AppHost and uploads the validated manifest, resolved package provenance, and
   runtime diagnostics with seven-day retention.

The resolved provenance artifact includes consumer, library, and producer commits; both local
package IDs/versions/SHA-256 hashes; the validated request manifest; and optional correlation
metadata.

## Local build

The AppHost intentionally requires locally packed core and preview-contract packages. Pack them into
`.preview-feed` with version `0.0.0-local`, save a valid manifest outside the repository, then
restore/build with:

```bash
dotnet restore src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  -p:ModularAppHostsVersion=0.0.0-local \
  -p:PreviewContractVersion=0.0.0-local
dotnet build src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --no-restore \
  -p:ModularAppHostsVersion=0.0.0-local \
  -p:PreviewContractVersion=0.0.0-local
```
