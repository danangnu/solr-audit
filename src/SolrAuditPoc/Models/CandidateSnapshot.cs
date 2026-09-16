namespace SolrAuditPoc.Models;

public sealed record CandidateSnapshot(
    string CandidateId,
    DateTimeOffset CapturedAt,
    Dictionary<string, FileState> Files);
