# Solr Audit POC

A .NET 10 proof of concept for event-driven candidate-folder reconciliation.

The project is intentionally **dry-run only**. It does not write to Solr.

## Goal

Replace broad continuous repository scanning with a narrower workflow:

`Windows audit event -> extract candidate ID -> reconcile only that candidate folder`

This design is safer than treating raw audit events as authoritative file operations because rename activity can also produce `DELETE` access events.

## Solution structure

```text
SolrAuditPocSolution/
├─ SolrAuditPoc.sln
├─ src/
│  └─ SolrAuditPoc/
│     ├─ Models/
│     ├─ Services/
│     ├─ Program.cs
│     └─ SolrAuditPoc.csproj
├─ samples/
│  └─ sample-events.txt
├─ docs/
│  └─ DESIGN.md
├─ .gitignore
└─ README.md
```

## Requirements

- Windows
- .NET 10 SDK
- Read access to the candidate test folder if live reconciliation is required

## Build

From the solution directory:

```powershell
dotnet restore
dotnet build SolrAuditPoc.sln
```

## Run with sample events

```powershell
dotnet run --project .\src\SolrAuditPoc\SolrAuditPoc.csproj -- --input .\samples\sample-events.txt
```

## Run against the FLOSVR01 test root

```powershell
dotnet run --project .\src\SolrAuditPoc\SolrAuditPoc.csproj -- `
  --input .\samples\sample-events.txt `
  --root "C:\Shares-DFS\FastTrack\Candidate\To 1189999"
```

The POC only enumerates file metadata. It does not modify candidate files.

## State

Per-candidate snapshots are stored by default under:

```text
src\SolrAuditPoc\bin\<Configuration>\net10.0\state\
```

You can override this with:

```powershell
--state-dir "C:\SolrAuditPoc\state"
```

## Current behavior

The application:

1. Reads copied Kiwi/Windows Security Audit messages.
2. Parses object paths under the configured candidate test range.
3. Extracts candidate IDs.
4. Debounces multiple audit messages into one reconciliation request per candidate.
5. Enumerates the current files under that candidate folder.
6. Compares the current state with the previous snapshot.
7. Reports:
   - `ADD`
   - `REMOVE`
   - `CHANGE`
8. Saves the new snapshot.
9. Performs no Solr writes.

## Next phases

1. Live syslog/Kiwi input adapter.
2. Durable work queue.
3. Retry handling and service restart recovery.
4. Current Solr/index inventory adapter.
5. Separate test Solr collection.
6. Controlled Solr update adapter.
7. Periodic full reconciliation as a safety net.
