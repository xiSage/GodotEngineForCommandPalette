## Agent skills

### Issue tracker

Issues live as GitHub issues in `xiSage/GodotEngineForCommandPalette`, using the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default label vocabulary (role = label): `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one root `CONTEXT.md` + `docs/adr/`. See `docs/agents/domain.md`.

## Build & test

The main project requires an explicit runtime identifier; bare `dotnet build`/`dotnet test` fail with NETSDK1097 (`PublishSingleFile` without a RID). The solution-level build also rejects `-r` (NETSDK1134), so run at project level:

```
dotnet build GodotEngineForCommandPalette/GodotEngineForCommandPalette.csproj -r win-x64 -p:Platform=x64
dotnet test GodotEngineForCommandPalette.Tests/GodotEngineForCommandPalette.Tests.csproj -r win-x64 -p:Platform=x64   # runs the catalog tests
```
