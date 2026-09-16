namespace SolrAuditPoc.Models;

public sealed record FileState(
    string RelativePath,
    long Length,
    DateTime LastWriteUtc);
