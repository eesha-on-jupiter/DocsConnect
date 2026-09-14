# Authentication

DocsConnect uses OAuth 2.0 Bearer tokens. The plugin does not run its own OAuth flow — you
supply a valid access token, and every HTTP component sends it as `Authorization: Bearer {token}`.

## Scopes you need

| Scope | Needed for |
|---|---|
| `https://www.googleapis.com/auth/documents` | All Docs API calls: get/create/batchUpdate |
| `https://www.googleapis.com/auth/drive.readonly` (or `drive.metadata.readonly`) | [Get Document](components/Document/GetDocument.md)'s optional `Name` lookup, which resolves a document by name via Drive's `files.list` |
| `https://www.googleapis.com/auth/drive.file` | Alternative to the readonly Drive scopes above for lookup, but only sees files the app itself created — insufficient for looking up pre-existing documents by name. Also the scope [Get Image URL](components/Insert/GetImageUrl.md) needs when hosting a Bitmap or File Path: it uploads a new Drive file and sets it link-public, both of which `drive.file` covers for files the app itself created |

If you only ever pass a `Document ID` directly (never the `Name` input on Get Document) and never
host an image through Get Image URL (URL passthrough only), the `documents` scope alone is enough.

## Getting a token

The quickest way to get a short-lived Bearer token for testing is
[Google OAuth 2.0 Playground](https://developers.google.com/oauthplayground/):

1. Open the Playground and select the scopes above under "Google Docs API v1" (and "Drive API v3"
   if you need name lookup).
2. Authorize, then exchange the authorization code for tokens.
3. Copy the **Access token** — this is what goes into every component's `Token` input.

Access tokens from the Playground expire in about an hour. For a longer-lived setup, register your
own OAuth client and run a standard installed-app or service-account flow, then feed the resulting
access token into the plugin the same way.

## Wiring the token in

Every HTTP component ([Connect](components/Connect/index.md) and
[Document](components/Document/index.md) tabs) takes a `Token` input. Wire one Panel with your
token into all of them, or read it from an environment variable via a Grasshopper script component
if you'd rather not paste it onto the canvas directly.

## Verifying it works

[DocsConnectAuth](components/Connect/DocsConnectAuth.md) (Connect tab) calls Google's
`tokeninfo` endpoint and returns the granted `scope` list plus `expires_in` on its `Status` /
`Response` outputs. Run this first on any new canvas — if it fails, nothing downstream will work
either.

## What "Status" means

Every HTTP component reports a `Status` output. A successful call reports the HTTP status
(`200 OK`) and outcome; a failed call reports the HTTP error code and Google's error message body
(e.g. `401` for an expired/invalid token, `403` for a missing scope). Read `Status` before trusting
`Response` on any HTTP component.
