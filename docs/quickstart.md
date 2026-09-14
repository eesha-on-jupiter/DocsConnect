# Quickstart

Five components, one canvas: validate a token, create a document, insert some styled text, and
push it live.

## 1. Get a token

Follow [authentication.md](authentication.md) to get an OAuth 2.0 Bearer token with the
`https://www.googleapis.com/auth/documents` scope. Wire it into a Grasshopper Panel so you can
reuse it across components.

## 2. Verify the connection

Drop **Connect > Auth** ([DocsConnectAuth](components/Connect/DocsConnectAuth.md)).
Wire your token Panel into `Token`, then read `Status` / `Response` — a healthy response reports
the granted scopes and an `expires_in` value.

## 3. Create a document

Drop **Document > Create Document** ([CreateDocument](components/Document/CreateDocument.md)).
Wire your token in, set `Title`, and click the button on the component to fire the create. Its
`Document ID` output is what everything downstream chains from.

## 4. Author some content

Drop **Text > Insert Text** ([InsertText](components/Text/InsertTextJson.md)) with your first line
of text, then **Text > Styled Text** ([UpdateTextStyle](components/Text/UpdateTextStyleJson.md))
with `Bold = true` for a second line. Both are self-contained — neither takes a document index.

Feed both into **Combine > Request Aggregator**
([RequestAggregator](components/Combine/RequestAggregator.md)) — each component on its own wire,
into the `Request JSON` input, in the order you want them to appear — one wire each, or both
through a Merge. Its label should read `2 blocks`.

## 5. Push it live

Drop **Document > Batch Update** ([BatchUpdateDocument](components/Document/BatchUpdateDocument.md)).
Wire in your Token, the `Document ID` from step 3, and the `Requests JSON` from Request Aggregator.
Click the button — `Status` should report success, and the document now has your content.

## What's next

- See [examples/](examples/) for a complete worked canvas combining a captured Rhino viewport image
  with generated report text.
- [components/](components/) has one page per component with full input/output tables.
