# Auth

Validates one or more Google OAuth 2.0 tokens by checking each against Google's tokeninfo endpoint and returns its status and granted scopes.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Token | string (list) | Yes | — | The OAuth 2.0 bearer token(s) to validate. A list checks one token per item. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Status | string (list) | HTTP status of each token check, index-matched to Token. |
| Response | string (list) | Raw response body from each tokeninfo call, index-matched to Token, including granted scopes. |

## API endpoint

`GET https://oauth2.googleapis.com/tokeninfo`

## Example response

```json
{
  "scope": "https://www.googleapis.com/auth/documents",
  "expires_in": 3399
}
```

## Notes

Google's tokeninfo endpoint accepts the access token via either an `?access_token=` query parameter or an `Authorization: Bearer` header; this component uses the Bearer header for consistency with the rest of the client. It returns the token's granted scopes, which are surfaced as the Status/Response outputs.

Auth is the first component to place on any canvas: every other component in this plugin needs a valid token, so downstream components depend on a successful result from this one.

Feed a list of Token to validate several tokens in one call — each token is checked in turn, and one invalid or expired token does not stop the rest. Status and Response are index-matched to Token, so each entry reports its own connection outcome independently.
