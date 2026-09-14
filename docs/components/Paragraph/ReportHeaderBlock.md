# Report Header Block

Builds a standard project title block — project name, project number, date, author, and revision — producing one self-indexed sequence of `insertText` and `updateParagraphStyle` requests.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Project Name | string | No | "" | Project name text, inserted in TITLE style. Unlike Project Number/Author/Revision, this line is always inserted — left blank, it still produces an empty TITLE-styled paragraph. |
| Project Number | string | No | "" | Project number line. Left blank, this line is omitted entirely (no blank "Project No.:" line). |
| Date | string | No | "" | Date line. Defaults to today's date (`yyyy-MM-dd`) if left blank — unlike the other optional lines, Date is never omitted. |
| Author | string | No | "" | Author line. Left blank, this line is omitted entirely. |
| Revision | string | No | "" | Revision line. Left blank, this line is omitted entirely. |
| Layout | string | No | "lines" | Layout mode for arranging the project number/date/author/revision fields. Only `"lines"` is implemented in this version — any other value currently falls back to the same lines layout. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The full sequence of requests for the title block, already assembled into one requests-array string and self-indexed from 1 — a single item, not a list. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This component was moved here from the removed Report Tools tab. It is a composite: it builds the project name in TITLE style, followed by the project number, date, author, and revision as lines, self-indexed from 1. If Date is left blank, it defaults to today's date. Project Number, Author, and Revision are each skipped entirely (not inserted as a blank line) when left blank.

Despite the similar name, this component is unrelated to the page header/footer segments on the Document tab — it builds a title block inside the main body of the document, not a repeating page header.

## Related

- [Report Skeleton](ReportSkeleton.md) — a separate composite for building headings and body sections, often placed after a Report Header Block.
