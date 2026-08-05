# Aspire Modular AppHosts preview consumer

This repository is the trusted E2E consumer (Repo D) for the cross-repository module preview
sample. Its `main`-branch workflow validates an immutable preview manifest, checks out only the
allowlisted producer at its exact commit, packs that commit's module contract into an ephemeral
local NuGet feed, and starts the consumer AppHost against the selected producer source.

The fixture demonstrates two independent pins:

- `Shirubasoft.Aspire.ModularAppHosts` stays on the released exact version `1.1.0`.
- `Shirubasoft.Aspire.ModularAppHosts.PreviewProducer.Contract` is packed at a unique, exact
  per-run preview version from the validated producer commit.

The producer repository and revision are passed to the released library through:

```text
Aspire__ModularAppHosts__Modules__preview-producer__Repository
Aspire__ModularAppHosts__Modules__preview-producer__RepositoryRevision
```

## Trigger the E2E workflow

The required `manifest_json` input uses the preview tool's canonical schema. The fixture accepts
only `https://github.com/shirubasoft/aspire-modular-apphosts-preview-producer.git`, module
`preview-producer`, a clean producer snapshot, and a lowercase full 40- or 64-hex commit ID.
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

Save it as `preview-manifest.json`, then dispatch the trusted workflow from `main`:

```bash
gh workflow run module-preview-e2e.yml \
  --repo shirubasoft/aspire-modular-apphosts-preview-consumer \
  --ref main \
  -f manifest_json="$(jq -c . preview-manifest.json)" \
  -f correlation_id="local-$(date +%s)"
```

`callback_metadata_json` is optional opaque JSON. It is retained in diagnostics and provenance but
is never interpreted as a URL or invoked by the workflow.

## What the workflow proves

The workflow:

1. Runs only from the trusted `main` ref with `contents: read` and no repository secrets.
2. Validates the repository, module, clean state, and immutable commit before checkout.
3. Checks out producer source with credentials persistence disabled.
4. Packs the known producer contract project under a unique exact preview version.
5. Restores the consumer against that exact package from the local feed.
6. Starts only with the pinned Aspire CLI, then waits for both `preview-producer-api` and the
   feature-only `preview-producer-sidecar`.
7. Discovers the API URL through `aspire describe` and requires the exact response
   `preview-from-feature-branch`.
8. Always stops the AppHost and uploads the validated manifest, resolved package provenance, and
   runtime diagnostics with seven-day retention.

The resolved provenance artifact includes consumer and producer commits, preview contract package
ID/version/SHA-256, the validated request manifest, and optional correlation metadata.

## Local build

The AppHost intentionally requires a locally packed preview contract. Pack the producer contract
into `.preview-feed` with version `0.0.0-local`, then restore/build with:

```bash
dotnet restore src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  -p:PreviewContractVersion=0.0.0-local
dotnet build src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --no-restore \
  -p:PreviewContractVersion=0.0.0-local
```
