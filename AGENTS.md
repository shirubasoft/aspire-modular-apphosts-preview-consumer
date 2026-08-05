# Repository instructions

- Never synchronously block on asynchronous work. Propagate async work to the caller and use `await`.
- Keep `.github/workflows/module-preview-e2e.yml` on a reviewed trusted ref. This sample uses
  `codex/tool-owned-preview-materialization`; switch the gate and dispatch ref to `main` after merge.
- Do not broaden the producer allowlist or interpolate manifest fields into shell scripts.
- Resolve requested contracts only from the consumer-policy-owned GitHub Packages source. Keep the
  source fallback disabled; if it is deliberately restored later, producer source must be selected
  by a validated full commit ID, never by a branch name.
- Keep the image-only contract baseline and all NuGet source credentials consumer-owned. A producer
  may select only the exact version of its allowlisted contract package, never a package source.
- Keep the preview implementation pinned to an exact trusted source commit. Pack the core and tool
  under unique per-run local versions; never accept their repository, commit, or version from the
  producer request.
- Start the AppHost only through the pinned Aspire CLI and always stop it in workflow cleanup.
