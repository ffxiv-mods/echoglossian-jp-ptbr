# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Echoglossian is a real-time FFXIV text-translation plugin for Dalamud (.NET 10, `Dalamud.NET.Sdk`, x64 only). The canonical agent rules (priorities, translation modes, performance, data, debugging, code style, release/issue workflow) live in AGENTS.md and are imported here. Follow them; this file adds orientation only.

@AGENTS.md

## This fork (EchoglossianJP)

This repo is the `ffxiv-mods/echoglossian-jp-ptbr` fork. It differs from upstream in a few ways:

- **Name:** `AssemblyName`/`InternalName` is `EchoglossianJP` (manifest `EchoglossianJP.json`, XML docs `EchoglossianJP.xml`), while `RootNamespace` stays `Echoglossian` so embedded resource names keep working.
- **Commands:** slash commands are `/eglojp`, `/eglojpdbmanager` and `/eglojptranslatordebugger`, so the fork can sit next to the official plugin.
- **Japanese source option:** `Config.UseJapaneseSourceText` sends the Japanese original, read from the Lumina sheets for the same row, to the engine while the client runs in another language.
  - Only the engine input changes. Operation scopes, persisted originals and lookups stay in the client language, and there is no migration.
  - Code lives in `GeneralHelpers/SourceText/` (policy and fallback translation) and `NativeUI/Helpers/SourceText/` (Japanese quest provider and plugin wiring).
  - Covered surfaces: Talk/BattleTalk lines from accepted quests, the accepted-quest prefetch (Journal, JournalDetail, ScenarioTree, `_ToDoList`), JournalAccept/JournalResult/RecommendList titles, and item/action/trait/reference-text prefetches. Everything else falls back to the client text.
- **Release:** `.github/workflows/release-custom-repo.yml` builds on push. On manual dispatch it also creates a release and commits `repo.json` for Dalamud custom repositories.
- **DalaMock:** the vendored `vendor/DalaMock` currently fails to compile against newer local Dalamud dev builds, because of missing interface members.

## Build and test

Build and test projects reference Dalamud, Lumina and FFXIVClientStructs DLLs from `%AppData%\XIVLauncher\addon\Hooks\dev` (`DalamudLibPath`). If that folder is missing, the build fails. `Directory.Build.props`/`.rsp` force `Platform=x64`.

```powershell
dotnet build Echoglossian.sln -c Debug --no-restore
dotnet build Echoglossian.Tests\Echoglossian.Tests.csproj -c Debug --no-restore
dotnet test  Echoglossian.Tests\Echoglossian.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ActionMenuWindowHandlerTests"
```

- Always run tests with a `--filter` (a class or method). Run one test command at a time, never in parallel.
- The tests use xUnit and FluentAssertions.
- DalaMock-hosted integration tests: `dotnet test Echoglossian.Mock.Tests\Echoglossian.Mock.Tests.csproj -c Debug --no-build -p:VSTestMaxCpuCount=1`. They need local FFXIV game data, resolved from `EXD_DATA_DIR`, then the XIVLauncher config, then the default install paths.
- `scripts\validate-local-tests.ps1` runs the full local rail: the sync-DB hot-path audit plus every test project. It is heavy, so use it only when you need to.
- `dotnet run --project .\Echoglossian.Mock\Echoglossian.Mock.csproj -c Debug` starts the local mock host.
- `Echoglossian.Previewer` is a standalone Veldrid/ImGui host for previewing plugin windows outside the game.

Other recurring scripts are in `scripts/`:
- `add-ef-migration.ps1 -MigrationName X` scaffolds an EF Core migration. It builds the plugin and copies in the Dalamud DLLs it needs.
- `update-translation-surface-docs.ps1` regenerates `docs/translation-surface-*`.
- `inspect-eglo-db.ps1` and `mutate-eglo-db.ps1` work with the plugin's SQLite DB.
- `audit-sync-db-hotpaths.ps1` checks the sync-DB hot-path baseline. CI also runs it.
- `new-d17-pr-body.ps1` writes the body for D17 manifest PRs.

## Architecture overview

- **Entry point**: `Echoglossian.cs` (a `partial` class split across many files) implements `IDalamudPlugin`. It registers chat commands (`/eglo`, `/eglodbmanager`, `/egloaddonprobe`, quest probe, etc.), builds the `PersistenceCoordinator`, fonts, the overlay renderer, and the `TranslationService`. User settings are in `Config.cs`. Startup wiring is in `PluginRuntime/Startup`.
- **Capture → translate → render/apply pipeline**:
  - `NativeUI/AddonHandlers/<Family>/` holds the per-addon runtimes that capture text: Talk, Toasts, Quest, ActionMenu, MainMenu, Character, SelectionDialogs, and others.
  - `NativeUI/Handlers` and `NativeUI/Helpers` hold the shared helpers. These include the DB-first GameWindow base (`AddonHandlers/Common/DbFirstGameWindowAddonHandler.cs`) and the structured StringArrayData helpers.
  - Translation always goes through `Translators/TranslationService`. Engines implement `ITranslator`; `TranslatorFactory` and `TranslatorEngineMap` select them. LLM engines (OpenAI/ChatGPT, Claude, Gemini, DeepSeek, OpenRouter, Ollama, LM Studio) add structured-dialogue requests, a capability matrix and surface-group routing.
  - `UIOverlays/TranslationOverlay` and `UIOverlays/TextPresentation` render overlays, including RTL.
- **Three ownership families** (do not mix them):
  1. Live GameWindow addons (ActionMenu, `_MainCommand`, ContextMenu, SystemMenu) use the DB-first GameWindow base.
  2. Character-family windows use structured StringArrayData payloads.
  3. Sheet-backed canonical/reference text (Lumina sheets such as MainCommand and Action/Item tooltips) is stored in reference-text caches and tables.
- **Persistence**:
  - EF Core SQLite: `EFCoreSqlite/` (models, `EchoglossianDbContext`, and `Migrations/` with timestamped names).
  - Writes are queued through `Persistence/PersistenceCoordinator`, a bounded priority queue, so the game thread never blocks on the DB. Persistence helpers are in `DBHelpers/`.
  - `Cache/` holds the in-memory managers that suppress repeated work. The DB is the source of truth.
  - Many translation rows are keyed by `TranslationReuseScope`: source, target, content/version and engine.
- **Localization**: plugin UI strings come from `Properties/Resources.resx` and are accessed as `Resources.Key`. Crowdin generates `Properties/Resources.<locale>.resx`; never hand-edit them. See `docs/localization-build-flow.md`.
- **Generated XML docs**: `Echoglossian.xml` is generated by the build and committed with code changes.

## Path-scoped rules and deeper docs

- `.github/instructions/*.instructions.md` holds per-area rules, matched by `applyTo` globs. Read the matching file before editing these areas:
  - ActionMenu, MainMenu, the shared GameWindow base, Character, and the StringArrayData infrastructure/persistence code.
  - Runtime validation (`validation.instructions.md`).
- `.agents/skills/` holds workflow guides for GameWindow and StringArrayData work.
- `docs/` holds the runtime flow maps. Start with:
  - `translation-surface-runtime-map.md`
  - `translation-engines-architecture-and-flows.md`
  - `dialogue-and-toast-runtime-flows.md`
  - `release-workflow.md`
- Runtime log: `<PluginConfigDirectory>\Echoglossian.log`, written via `PluginRuntimeLog`.
