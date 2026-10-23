# Aspire Modular AppHosts preview consumer

> [!IMPORTANT]
> This repository is archived. It was a fixture for the cross-repository module preview sample of
> `aspire-modular-apphosts`, whose packages continue as the
> [Multirepo extension](https://github.com/shirubasoft/aspire-extensions/tree/main/extensions/Shirubasoft.Aspire.Extensions.Multirepo)
> in shirubasoft/aspire-extensions.

This repository is the trusted E2E consumer (Repo D) for the cross-repository module preview
sample. Its reviewed workflow verifies an immutable producer request, materializes only artifacts
allowed by consumer policy, and starts the consumer AppHost with the resulting trusted resolution.
Repo D never builds the producer's runtime images: it inspects and runs the exact OCI digests that
the producer already published.

The fixture supports two contract modes:

- An image-only request omits `contracts`. The consumer keeps its own exact baseline,
  `Shirubasoft.Aspire.ModularAppHosts.PreviewProducer.Contract` version `2.0.0-preview.7.1`.
- A published-contract request selects an exact version of that allowlisted package. The consumer
  downloads and hashes it from its policy-owned GitHub Packages source. Source fallback is disabled,
  so materialization cannot check out or pack producer source.

Both modes require immutable API and nginx sidecar images from the repositories allowlisted in
[`module-preview-policy.json`](.github/module-preview-policy.json). The AppHost receives only the
trusted resolution path and applies it before module import:

```csharp
await builder.ApplyModulePreviewResolutionAsync(previewResolution);
await PreviewProducerModule.ImportModuleAsync(builder);
```

The workflow owns three independent trust boundaries:

- It checks out an exact `aspire-modular-apphosts` commit, packs the runtime as the shared immutable
  ABI version `1.2.0-preview.f54233a`, and packs the tool under a unique job-local version. The
  producer cannot select this implementation.
- It owns the allowed contract package ID, GitHub Packages source, image repositories, baseline
  version, package credentials, and whether a contract request is optional.
- The producer selects only its clean pushed commit, an optional exact contract version, and exact
  OCI digests in those approved repositories.

The workflow currently pins library commit
[`f54233a36d4c2ffbe532533d0b1867faf1b42d89`](https://github.com/shirubasoft/aspire-modular-apphosts/commit/f54233a36d4c2ffbe532533d0b1867faf1b42d89).

## Prerequisites

Before dispatching the E2E:

1. Publish the API image to the allowlisted GHCR repository and retain its digest. The fixture's
   nginx sidecar is also selected by digest.
2. For a published-contract request, publish the requested exact package version to
   `https://nuget.pkg.github.com/shirubasoft/index.json` before dispatch.
3. Grant this consumer repository Actions read access to that GitHub Packages package. The workflow
   grants its job token only `contents: read` and `packages: read`.
4. Keep `.github/workflows/module-preview-e2e.yml` on a reviewed trusted ref. This sample currently
   uses `codex/tool-owned-preview-materialization`; change the gate and dispatch ref to `main` after
   the PR merges.

The GHCR image must be public for the current no-registry-secret fixture. A private producer can add
narrowly scoped read-only registry authentication to this trusted workflow, but credentials must
never be placed in the preview request.

## Produce and trigger a preview

Run the preview tool from a clean producer branch whose HEAD is already pushed to `origin`. For a
published-contract request, the producer descriptor declares the contract package ID and CI supplies
the version it just published:

```bash
dotnet modular-apphosts preview produce \
  --descriptor module-preview.producer.json \
  --contract-version "$contract_version" \
  --image "preview-producer-api=$api_repository@$api_digest" \
  --image "preview-producer-sidecar=docker.io/library/nginx@$nginx_digest" \
  --output preview-manifest.json
```

For an image-only preview, use a descriptor without `contract` and omit `--contract-version`. The
generated manifest will contain no contract request, and Repo D will compile against its baseline
`2.0.0-preview.7.1` package while applying the requested image digests at runtime.

Dispatch the reviewed consumer workflow and wait for its result:

```bash
dotnet modular-apphosts preview trigger \
  --manifest preview-manifest.json \
  --repo shirubasoft/aspire-modular-apphosts-preview-consumer \
  --workflow module-preview-e2e.yml \
  --ref codex/tool-owned-preview-materialization \
  --input correlation_id=local-verification \
  --wait \
  --github-output "$GITHUB_OUTPUT"
```

The trigger prints `workflow_run_id=<id>` and `workflow_run_url=<url>`. In GitHub Actions it also
appends those values to `$GITHUB_OUTPUT`, and `--wait` makes the producer job fail if the consumer
run fails, is cancelled, or cannot be watched successfully.

`callback_metadata_json` is optional opaque JSON. It is retained in diagnostics but is never
interpreted as a URL or invoked by the workflow.

## Consumer policy and package authentication

The checked-in policy makes the contract optional and pins its only published source:

```json
{
  "contract": {
    "packageId": "Shirubasoft.Aspire.ModularAppHosts.PreviewProducer.Contract",
    "versionEnvironment": "PreviewContractVersion",
    "required": false,
    "published": {
      "source": "https://nuget.pkg.github.com/shirubasoft/index.json"
    },
    "sourceFallback": {
      "enabled": false
    }
  }
}
```

The request cannot supply or override the source. The workflow adds credentials to its checked-out
copy of `nuget.config` using the consumer job's `GITHUB_TOKEN`; credentials are neither committed nor
uploaded as diagnostics. `nuget.config` retains the local `.preview-feed` used for the pinned core
and downloaded contract, the GitHub Packages source used by image-only baseline restore and
published-package resolution, and nuget.org for third-party dependencies.

## What the workflow proves

The workflow:

1. Runs only from the reviewed sample ref with `contents: read` and `packages: read`.
2. Packs and installs the workflow-owned runtime and preview tool from an exact trusted commit.
3. Verifies the request against policy before any producer artifact materialization.
4. Uses no package feed, NuGet configuration, Git command, or contract pack for image-only
   materialization; its resolution has zero contracts and the baseline remains unchanged.
5. For a contract request, downloads only the requested allowlisted ID and exact version from the
   policy-owned source, copies it into `.preview-feed`, and records its source, SHA-256, and absolute
   package path. Guard executables fail the job if materialization attempts Git or `dotnet pack`.
6. Verifies both OCI digests, restores the consumer, and applies the trusted resolution before module
   import.
7. Starts only with the pinned Aspire CLI, waits for both resources, and requires the API response
   `preview-from-feature-branch` plus the expected nginx page.
8. Always stops the AppHost and uploads the validated request, toolchain provenance, resolution,
   package provenance, and runtime diagnostics with seven-day retention.

The untrusted manifest is never passed to the AppHost. Only the verified resolution and the
policy-approved package-version environment reach startup.

## Local build

Local restore needs a locally packed core version and read access to the contract package source.
Authenticate a checked-out copy of `nuget.config` with a token that has `read:packages`:

```bash
dotnet nuget update source github-shirubasoft \
  --configfile "$PWD/nuget.config" \
  --username "$GITHUB_USER" \
  --password "$GITHUB_PACKAGES_TOKEN" \
  --store-password-in-clear-text
```

Do not commit the resulting credentials. The AppHost defaults to baseline contract version
`2.0.0-preview.7.1`; set `PreviewContractVersion` only when testing a materialized published request:

```bash
export ModularAppHostsVersion=1.2.0.1
export ModulePreview__Resolution="$PWD/diagnostics/local-resolution.json"

dotnet restore src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --configfile "$PWD/nuget.config" \
  -p:ModularAppHostsVersion="$ModularAppHostsVersion"
dotnet build src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --no-restore \
  -p:ModularAppHostsVersion="$ModularAppHostsVersion"
```

Start the AppHost with the same lifecycle as CI:

```bash
aspire start \
  --apphost src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --non-interactive
aspire wait preview-producer-api \
  --apphost src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --non-interactive
aspire wait preview-producer-sidecar \
  --apphost src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --non-interactive
aspire stop \
  --apphost src/Preview.Consumer.AppHost/Preview.Consumer.AppHost.csproj \
  --non-interactive
```
