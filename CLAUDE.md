# AddRem

A .NET library that enumerates installed programs on Windows the way
Settings > Apps & Features does — registry Uninstall keys (MSI and EXE) plus
MSIX/Store packages — unified into one `InstalledProgram` model carrying install
scope, architecture, and source. Callers get one list instead of hand-polling
four registry hive/view combinations and guessing which entries are user-visible.

Ships as NuGet packages (`AddRem`, `AddRem.Msix`) plus a console demo.

## Layout

| Path | What |
|---|---|
| `src/AddRem` | Core library. `net10.0-windows`. **Zero NuGet dependencies.** |
| `src/AddRem.Msix` | MSIX/Store source. `net10.0-windows10.0.19041.0` (WinRT projection). |
| `src/AddRem.Demo` | Console demo and snapshot-capture tool. Not packable. |
| `tests/AddRem.Tests` | Unit tests. **Must never touch the live registry.** |
| `tests/AddRem.IntegrationTests` | Live-machine tests. Separate CI job. |

## Workflow — follow this for every change

1. **Branch. Never commit to `main`.**
   `git switch -c feat/<short-name>` (also `fix/`, `chore/`, `docs/`)
2. **Every change ships with tests.** Writing the failing test first is
   encouraged — do it whenever it's natural — but it is not a hard red-green
   gate. New public API without a covering test is not done.
3. Implement.
4. **Before pushing, in this order:**
   ```
   dotnet tool restore
   dotnet csharpier check .
   dotnet build AddRem.slnx -c Release -warnaserror
   dotnet test --project tests/AddRem.Tests/AddRem.Tests.csproj -c Release
   ```
   If the format check fails, run `dotnet csharpier format .` and re-stage.
5. **Push and open a PR into `main`.** CI must be green before merge.

One-time setup in a fresh clone:
```
git config core.hooksPath .githooks
dotnet tool restore
```
`core.hooksPath` is local config and cannot be committed, so this step is
per-clone. The hook runs the same four commands as step 4.

## Commands

| Task | Command |
|---|---|
| Restore local tools | `dotnet tool restore` |
| Check formatting | `dotnet csharpier check .` |
| Fix formatting | `dotnet csharpier format .` |
| Build (strict) | `dotnet build AddRem.slnx -c Release -warnaserror` |
| Unit tests | `dotnet test --project tests/AddRem.Tests/AddRem.Tests.csproj -c Release` |
| Integration tests | `dotnet test --project tests/AddRem.IntegrationTests/AddRem.IntegrationTests.csproj -c Release` |
| All tests | `dotnet test --solution AddRem.slnx -c Release` |
| Run the demo | `dotnet run --project src/AddRem.Demo -- list --all` |
| Capture a fixture | `dotnet run --project src/AddRem.Demo -- snapshot --redact -o tests/AddRem.Tests/Fixtures/<name>.json` |
| Pack locally | `dotnet pack src/AddRem -c Release -o artifacts` |

## Conventions

- **Tests run on the Microsoft Testing Platform**, opted into via `global.json`.
  `dotnet test` therefore takes `--project` / `--solution` rather than a
  positional path, and extension options (`--report-xunit-trx`, `--coverage`)
  are passed directly with no `--` separator. VSTest syntax will not work.
- **CSharpier owns formatting.** Do not hand-format; do not argue with it.
  `.editorconfig` carries only what CSharpier does not: encoding, naming,
  `var` usage, using placement, analyzer severities.
- File-scoped namespaces. Nullable enabled everywhere; no `!` without a comment
  justifying it. Implicit usings on.
- Public types and members get XML doc comments. `GenerateDocumentationFile` is
  on, so a missing one is a warning, and CI builds with `-warnaserror`.
- `record` for model types, `readonly record struct` for small values, `sealed`
  by default on classes.
- **Parsing is pure.** Anything that parses a registry value, a version, a date,
  an icon reference or an uninstall command is a `static bool TryParse` with no
  I/O, so it can be unit-tested from a table.
- **All registry access goes through `IRegistryRoot` / `IRegistryKey`.** Never
  call `Microsoft.Win32.Registry` outside `WindowsRegistryRoot`. This is the
  testability seam; breaking it breaks the test suite's premise, and a guard
  test in `AddRem.Tests` enforces it by reflection.
- Always dispose registry keys. Enumeration must not leak handles.
- **Never throw on a missing or malformed registry value** — degrade to `null`
  and keep going. One bad vendor entry must not break enumeration. (Real data
  has `EstimatedSize` as a string, `InstallDate` as a Unix epoch, and
  `DisplayIcon` with negative resource IDs.)
- Use explicit `RegistryView.Registry32` / `Registry64`, never the literal
  `Wow6432Node` path — mixing the two double-counts entries.

## Do not

- Do not commit directly to `main`.
- Do not push without `dotnet csharpier check .` and the unit tests passing.
- Do not add NuGet dependencies to `src/AddRem` — core stays dependency-free.
- Do not read the live registry from `tests/AddRem.Tests`.
- Do not make the library *execute* uninstalls. It parses and reports uninstall
  commands; running one is the caller's decision. In particular, never guess a
  silent switch for an arbitrary vendor EXE.
- Do not commit fixtures containing usernames, machine names, SIDs or license
  keys — capture them with `snapshot --redact`.
- Do not casually bump the `global.json` SDK pin. It is pinned to the 10.0.1xx
  band because the `windows-latest` runner image currently ships a 10.0.2xx SDK
  that its MSBuild cannot run. CI depends on this.
