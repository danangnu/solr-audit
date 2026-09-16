# Design Notes

## Observed audit behavior

Manual testing on the configured FLOSVR01 candidate test range showed:

- File creation produced audit events.
- Content modification produced `WriteData` / `AppendData`.
- Rename activity produced `DELETE` access against the old filename.
- The renamed destination filename was not reliably present in the forwarded Kiwi event stream.

Therefore, a raw `DELETE` access event must **not** be treated as an instruction to delete a Solr document.

## POC strategy

Use audit events only as a trigger.

Example:

```text
Audit event mentions candidate 1180097
        |
        v
Queue candidate 1180097
        |
        v
Inspect current files under candidate folder
        |
        v
Compare with previous/indexed inventory
        |
        +--> Added files
        +--> Removed files
        +--> Changed files
```

This reduces repository scanning while retaining correctness for rename/delete ambiguity.

## Production considerations

Before any production Solr write integration:

- Use a durable queue.
- Deduplicate repeated events.
- Retry inaccessible folders.
- Never interpret an inaccessible folder as empty.
- Record every proposed index mutation.
- Use a separate Solr test collection first.
- Retain a scheduled reconciliation safety scan.
- Measure:
  - file change time
  - event arrival time
  - reconciliation completion time
  - Solr update acceptance time
  - search visibility time
