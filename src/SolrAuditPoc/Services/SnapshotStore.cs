using System.Text.Json;
using SolrAuditPoc.Models;

namespace SolrAuditPoc.Services;

public static class SnapshotStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string StatePath(
        string stateDirectory,
        string candidateId) =>
        Path.Combine(stateDirectory, $"{candidateId}.json");

    public static CandidateSnapshot? Load(
        string stateDirectory,
        string candidateId)
    {
        var path = StatePath(stateDirectory, candidateId);

        if (!File.Exists(path))
            return null;

        return JsonSerializer.Deserialize<CandidateSnapshot>(
            File.ReadAllText(path),
            JsonOptions);
    }

    public static void Save(
        string stateDirectory,
        CandidateSnapshot snapshot)
    {
        Directory.CreateDirectory(stateDirectory);

        File.WriteAllText(
            StatePath(stateDirectory, snapshot.CandidateId),
            JsonSerializer.Serialize(snapshot, JsonOptions));
    }
}
