# Aspire Modular AppHosts preview consumer

This repository is the trusted E2E consumer (Repo D) for the cross-repository module preview
sample. Its trusted-branch workflow validates an immutable preview manifest, checks out only the
allowlisted producer contract at its exact commit, verifies producer-built container digests, and
starts the consumer AppHost from a trusted resolution written by the preview tool.

The fixture demonstrates three independent pins:

- The trusted workflow owns an exact `aspire-modular-apphosts` commit, packs both its runtime and
  tool at unique versions, and uses that tool for request validation and materialization.
- `Shirubasoft.Aspire.ModularAppHosts.PreviewProducer.Contract` is packed at a unique, exact
  version from the validated producer commit through a policy-owned source fallback.
- The API and sidecar run from producer-selected digests in consumer-allowlisted repositories.

The AppHost receives only the trusted resolution path and applies it before module import:

```csharp
await builder.ApplyModulePreviewResolutionAsync(previewResolution);
await PreviewProducerModule.ImportModuleAsync(builder);
```

The untrusted request never reaches the AppHost. The resolution records the exact contract package
hash and OCI digests that the consumer tool verified.

The workflow currently pins library commit
[`8aea5cd6fccd2b9e71dfb391f7f72ef79ea4262a`](https://github.com/shirubasoft/aspire-modular-apphosts/commit/8aea5cd6fccd2b9e71dfb391f7f72ef79ea4262a).

## Trigger the E2E workflow

The required `manifest_json` input uses the preview tool's canonical schema. The fixture accepts
only the module, contract, and image identities in
[`.github/module-preview-policy.json`](.github/module-preview-policy.json). Both images are required,
so omitting a digest cannot cause Repo D to build runtime images from producer source.

The producer feature branch currently has this example manifest:

```json
{
  "schemaVersion": 1,
  "producer": {
    "repository": "https://github.com/shirubasoft/aspire-modular-apphosts-preview-producer.git",
    "commit": "61249f4f50d44332288ff74638e9261b1736e551",
    "dirty": false,
    "branch": "codex/preview-resource-change",
    "baseRef": "refs/heads/main",
    "baseCommit": "4ca008a6c8ecbe22f2c88ca8099f3b14c997b7c8"
  },
  "modules": [
    {
      "name": "preview-producer",
      "repository": "https://github.com/shirubasoft/aspire-modular-apphosts-preview-producer.git",
      "commit": "61249f4f50d44332288ff74638e9261b1736e551",
      "branch": "codex/preview-resource-change",
      "baseRef": "refs/heads/main",
      "baseCommit": "4ca008a6c8ecbe22f2c88ca8099f3b14c997b7c8"
    }
  ],
  "contracts": [
    {
      "module": "preview-producer",
      "packageId": "Shirubasoft.Aspire.ModularAppHosts.PreviewProducer.Contract",
      "version": "2.0.0-preview.1"
    }
  ],
  "images": [
    {
      "module": "preview-producer",
      "resource": "preview-producer-api",
      "resourceKind": "container",
      "repository": "ghcr.io/shirubasoft/aspire-modular-apphosts-preview-producer/api",
      "sha256": "sha256:5af6b0ec9fd3716b22b1469b7486258c89d105de9c57d933a5c5a025697061b3"
    },
    {
      "module": "preview-producer",
      "resource": "preview-producer-sidecar",
      "resourceKind": "container",
      "repository": "docker.io/library/nginx",
      "sha256": "sha256:5616878291a2eed594aee8db4dade5878cf7edcb475e59193904b198d9b830de"
    }
  ]
}
```

Save it as `preview-manifest.json`, then use the preview tool to dispatch the trusted workflow from
this sample branch:

```bash
dotnet modular-apphosts preview trigger \
  --manifest preview-manifest.json \
  --repo shirubasoft/aspire-modular-apphosts-preview-consumer \
  --workflow module-preview-e2e.yml \
  --ref codex/tool-owned-preview-materialization \
  --input correlation_id=local-verification
```

The GHCR package must be public before dispatch so this no-secrets consumer can inspect and pull its
digest. A private producer may instead add narrowly scoped, read-only registry authentication in the
trusted consumer workflow; never place registry credentials in the preview request.

`callback_metadata_json` is optional opaque JSON. It is retained in diagnostics and provenance but
is never interpreted as a URL or invoked by the workflow.

## What the workflow proves

The workflow:

1. Runs only from the trusted sample ref with `contents: read` and no repository secrets.
2. Packs and installs the workflow-owned preview runtime and tool from an exact commit.
3. Runs offline policy verification before any producer checkout or producer code execution.
4. Materializes only the policy-owned contract project from the exact producer commit, verifies its
   package identity and hash, and verifies both OCI digests without building runtime images.
5. Restores the consumer from the isolated local feed and applies the trusted resolution before
   importing the module.
6. Starts only with the pinned Aspire CLI, then waits for and probes both immutable container
   resources.
7. Requires the API response `preview-from-feature-branch` and the expected nginx sidecar page.
8. Always stops the AppHost and uploads the request, toolchain provenance, trusted resolution,
   package provenance, and runtime diagnostics with seven-day retention.

The resolution includes the consumer identity, request hash, module commit, contract package
identity/hash/path, and verified OCI digests. Optional correlation and callback metadata is retained
separately and never interpreted or invoked.

## Local build

The AppHost intentionally requires locally packed core and preview-contract packages. After running
`preview materialize` with this repository's policy and a valid request, set the versions used for
those packages and the absolute resolution path, then restore/build. For example:

```bash
export ModularAppHostsVersion=1.2.0.1
export PreviewContractVersion=2.0.0-preview.1
export ModulePreview__Resolution="$PWD/diagnostics/local-resolution.json"

dotnet restore src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  -p:ModularAppHostsVersion="$ModularAppHostsVersion" \
  -p:PreviewContractVersion="$PreviewContractVersion"
dotnet build src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --no-restore \
  -p:ModularAppHostsVersion="$ModularAppHostsVersion" \
  -p:PreviewContractVersion="$PreviewContractVersion"
```
