using System.Text.RegularExpressions;
using SolrAuditPoc.Models;

namespace SolrAuditPoc.Services;

public static class AuditParser
{
    private static readonly Regex ObjectNameRegex = new(
        @"Object Name:\s*(?<path>[A-Za-z]:\\Shares-DFS\\FastTrack\\Candidate\\To 1189999\\(?<candidate>\d+)(?:\\[^\r\n]*)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AccessRegex = new(
        @"Accesses:\s*(?<access>.*?)(?=\s+Access Mask:|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex ProcessRegex = new(
        @"Process Name:\s*(?<process>[A-Za-z]:\\[^\r\n]*?\.exe)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static AuditTrigger? Parse(string raw)
    {
        var objectMatch = ObjectNameRegex.Match(raw);
        if (!objectMatch.Success)
            return null;

        var path = objectMatch.Groups["path"].Value.Trim();
        var candidateId = objectMatch.Groups["candidate"].Value.Trim();
        var candidateFolder =
            $@"C:\Shares-DFS\FastTrack\Candidate\To 1189999\{candidateId}";

        string? accesses = null;
        var accessMatch = AccessRegex.Match(raw);
        if (accessMatch.Success)
            accesses = Regex.Replace(
                accessMatch.Groups["access"].Value,
                @"\s+",
                " ").Trim();

        string? processName = null;
        var processMatch = ProcessRegex.Match(raw);
        if (processMatch.Success)
            processName = processMatch.Groups["process"].Value.Trim();

        return new AuditTrigger(
            DateTimeOffset.Now,
            candidateId,
            candidateFolder,
            path,
            accesses,
            processName,
            raw);
    }
}
