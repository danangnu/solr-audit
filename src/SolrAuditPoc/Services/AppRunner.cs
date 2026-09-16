using System.Text.RegularExpressions;
using SolrAuditPoc.Models;

namespace SolrAuditPoc.Services;

public static class AppRunner
{
    public static int Run(string[] args)
    {
        var inputPath = GetArg(args, "--input");
        var rootOverride = GetArg(args, "--root");

        var stateDirectory = GetArg(args, "--state-dir")
            ?? Path.Combine(AppContext.BaseDirectory, "state");

        var debounceMsText = GetArg(args, "--debounce-ms");
        var debounceMs = int.TryParse(
            debounceMsText,
            out var parsedDebounce)
            ? parsedDebounce
            : 1500;

        Console.WriteLine("Solr Audit POC - DRY RUN ONLY");
        Console.WriteLine("No Solr writes are performed.");
        Console.WriteLine();

        var records = ReadRecords(inputPath);

        var triggers = records
            .Select(AuditParser.Parse)
            .Where(x => x is not null)
            .Cast<AuditTrigger>()
            .ToList();

        if (triggers.Count == 0)
        {
            Console.WriteLine(
                "No matching candidate audit events found.");
            return 0;
        }

        foreach (var trigger in triggers)
        {
            Console.WriteLine(
                $"TRIGGER candidate={trigger.CandidateId} " +
                $"access=\"{trigger.Accesses ?? "unknown"}\" " +
                $"object=\"{trigger.ObjectPath}\"");
        }

        Console.WriteLine();
        Console.WriteLine(
            $"Parsed {triggers.Count} matching event(s).");

        var grouped = triggers
            .GroupBy(t => t.CandidateId)
            .OrderBy(g => g.Key)
            .ToList();

        if (debounceMs > 0)
            Thread.Sleep(debounceMs);

        Console.WriteLine();
        Console.WriteLine("RECONCILIATION PLAN");

        foreach (var group in grouped)
        {
            ReconcileCandidate(
                group.Key,
                group.ToList(),
                rootOverride,
                stateDirectory);
        }

        Console.WriteLine();
        Console.WriteLine(
            "Dry run complete. Nothing was written to Solr.");

        return 0;
    }

    private static IEnumerable<string> ReadRecords(
        string? inputPath)
    {
        string text;

        if (!string.IsNullOrWhiteSpace(inputPath))
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException(
                    "Input file not found.",
                    inputPath);
            }

            text = File.ReadAllText(inputPath);
        }
        else
        {
            Console.WriteLine(
                "Paste Kiwi event message(s). " +
                "Press Ctrl+Z then Enter when finished.");

            text = Console.In.ReadToEnd();
        }

        return Regex.Split(
                text,
                @"(?:\r?\n){2,}")
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    private static void ReconcileCandidate(
        string candidateId,
        IReadOnlyCollection<AuditTrigger> triggers,
        string? rootOverride,
        string stateDirectory)
    {
        var folder = rootOverride is null
            ? triggers.First().CandidateFolder
            : Path.Combine(rootOverride, candidateId);

        if (!Directory.Exists(folder))
        {
            Console.WriteLine(
                $"Candidate {candidateId}: " +
                $"{triggers.Count} trigger(s) -> " +
                $"QUEUE RECONCILIATION for \"{folder}\" " +
                "[folder not accessible from this machine]");

            return;
        }

        var previous = SnapshotStore.Load(
            stateDirectory,
            candidateId);

        var current = SnapshotEngine.Capture(
            candidateId,
            folder);

        var result = SnapshotEngine.Compare(
            previous,
            current,
            folder,
            triggers.Count);

        Console.WriteLine(
            $"Candidate {candidateId}: " +
            $"{result.TriggerCount} trigger(s)");

        Console.WriteLine($"  Folder : {folder}");

        if (result.Added.Count == 0
            && result.Removed.Count == 0
            && result.Changed.Count == 0)
        {
            Console.WriteLine(
                "  Result : no inventory change detected");
        }
        else
        {
            foreach (var item in result.Added)
                Console.WriteLine($"  ADD    : {item}");

            foreach (var item in result.Removed)
                Console.WriteLine($"  REMOVE : {item}");

            foreach (var item in result.Changed)
                Console.WriteLine($"  CHANGE : {item}");
        }

        SnapshotStore.Save(
            stateDirectory,
            current);
    }

    private static string? GetArg(
        string[] args,
        string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(
                    args[i],
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
