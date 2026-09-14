# Replace All Text

Builds one or more `replaceAllText` Request JSON objects that find and replace every occurrence of a string.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Find | string (list) | Yes | — | Text to search for. A list builds one request per item. |
| Replace | string (list) | No | "" | Replacement text, index-matched to Find (a shorter list repeats its last value). |
| Match Case | bool | Yes | true | Whether the search is case-sensitive. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `replaceAllText` Request JSON object(s), index-matched to Find. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that goes into [Request Aggregator](../Combine/RequestAggregator.md)'s `Fixed Requests` input (it already carries its own indices, so it must not be re-placed) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

Unlike every other component on this tab, Replace All Text has no Segment ID input and never has had one. The underlying `replaceAllText` request has no location or segment field to scope it to one part of the document — it matches across the entire document, including the body and any headers, footers, and footnotes, by design.

Feed a list of Find and a list of Replace to build several replace requests at once, each pair index-matched. If Replace has fewer entries than Find, the last replacement repeats for the remaining pairs.
