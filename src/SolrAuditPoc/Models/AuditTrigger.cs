namespace SolrAuditPoc.Models;

public sealed record AuditTrigger(
    DateTimeOffset ReceivedAt,
    string CandidateId,
    string CandidateFolder,
    string? ObjectPath,
    string? Accesses,
    string? ProcessName,
    string Raw);
