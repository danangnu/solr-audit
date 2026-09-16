namespace SolrAuditPoc.Models;

public sealed record ReconcileResult(
    string CandidateId,
    string CandidateFolder,
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Changed,
    int TriggerCount);
