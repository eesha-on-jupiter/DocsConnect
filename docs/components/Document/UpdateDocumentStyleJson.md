# Update Document Style

Updates document-wide style properties such as page margins, page size, and background color.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Margin Top | double | No | 0.0 | Top page margin. |
| Margin Bottom | double | No | 0.0 | Bottom page margin. |
| Margin Left | double | No | 0.0 | Left page margin. |
| Margin Right | double | No | 0.0 | Right page margin. |
| Page Width | double | No | 0.0 | Page width. |
| Page Height | double | No | 0.0 | Page height. |
| Background | string | No | "" | Background color as `#RRGGBB` (or `RRGGBB`) hex, or comma-separated `R,G,B` / `R,G,B,A` 0-255 integers (blank to skip). An unparseable value is skipped and reported as a runtime warning on the component, rather than failing silently. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `updateDocumentStyle` request. |

## Output

This component never calls the API directly — it emits a single Request JSON object (a Google Docs `Request`) that goes into [Request Aggregator](../Combine/RequestAggregator.md)'s `Fixed Requests` input (it carries no document position, so there is nothing to place) before [Batch Update Document](BatchUpdateDocument.md) can push it live.

## Notes

Every input here is optional — only the fields you actually set are changed on the document; leaving an input at its default does not overwrite that property. Specifically, each margin/page-size field only applies when set strictly greater than 0 — a value of 0 (the default) or negative is treated the same as "not set" and omitted from the request, so there is no way to set a margin to exactly 0 through this component. Margins and page size are typically supplied in points. This component is index-free by nature (it has no document position to author), so it feeds Request Aggregator directly rather than going through Request Aggregator.

## Related

- [Request Aggregator](../Combine/RequestAggregator.md) — where this component's output is combined with everything else before Batch Update.
