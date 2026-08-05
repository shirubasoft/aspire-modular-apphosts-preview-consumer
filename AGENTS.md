# Repository instructions

- Never synchronously block on asynchronous work. Propagate async work to the caller and use `await`.
- Keep `.github/workflows/module-preview-e2e.yml` on `main`; it is the trusted consumer workflow.
- Do not broaden the producer allowlist or interpolate manifest fields into shell scripts.
- Producer source must be selected by a validated full commit ID, never by a branch name.
- Keep the consumer on the exact released `Shirubasoft.Aspire.ModularAppHosts` 1.1.0 package.
- Start the AppHost only through the pinned Aspire CLI and always stop it in workflow cleanup.
