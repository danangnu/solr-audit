using SolrAuditPoc.Models;

namespace SolrAuditPoc.Services;

public static class SnapshotEngine
{
    public static CandidateSnapshot Capture(string candidateId, string folder)
    {
        var files = new Dictionary<string, FileState>(
            StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(folder))
        {
            return new CandidateSnapshot(
                candidateId,
                DateTimeOffset.Now,
                files);
        }

        foreach (var file in Directory.EnumerateFiles(
                     folder,
                     "*",
                     SearchOption.AllDirectories))
        {
            var info = new FileInfo(file);
            var relative = Path.GetRelativePath(folder, file);

            files[relative] = new FileState(
                relative,
                info.Length,
                info.LastWriteTimeUtc);
        }

        return new CandidateSnapshot(
            candidateId,
            DateTimeOffset.Now,
            files);
    }

    public static ReconcileResult Compare(
        CandidateSnapshot? previous,
        CandidateSnapshot current,
        string folder,
        int triggerCount)
    {
        previous ??= new CandidateSnapshot(
            current.CandidateId,
            DateTimeOffset.MinValue,
            new Dictionary<string, FileState>(
                StringComparer.OrdinalIgnoreCase));

        var added = current.Files.Keys
            .Except(previous.Files.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var removed = previous.Files.Keys
            .Except(current.Files.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var changed = current.Files.Keys
            .Intersect(previous.Files.Keys, StringComparer.OrdinalIgnoreCase)
            .Where(key =>
            {
                var before = previous.Files[key];
                var after = current.Files[key];

                return before.Length != after.Length
                       || before.LastWriteUtc != after.LastWriteUtc;
            })
            .OrderBy(x => x)
            .ToList();

        return new ReconcileResult(
            current.CandidateId,
            folder,
            added,
            removed,
            changed,
            triggerCount);
    }
}
