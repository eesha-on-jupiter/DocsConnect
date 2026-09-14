using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // HTTP client for the Google Docs API — the only file that touches HttpClient
    // Every method is built from spec/call-library-validated.json (4 entries VALIDATED live; find_documents_by_name,
    // upload_image_to_drive and set_file_public are UNVALIDATED — implemented but not yet run against a live token)
    public sealed class DocsConnectClient
    {
        #region Constructor and setup

        // Docs API host; auth_check uses an absolute tokeninfo URL on a different host
        private const string DocsBaseUrl = "https://docs.googleapis.com";
        private const string TokenInfoUrl = "https://oauth2.googleapis.com/tokeninfo";
        // Drive API host — the Docs API has no search/list, so name lookup goes through Drive files.list
        private const string DriveFilesUrl = "https://www.googleapis.com/drive/v3/files";
        private const string DriveUploadUrl = "https://www.googleapis.com/upload/drive/v3/files";

        private readonly HttpClient _http;
        private readonly string _token;

        public DocsConnectClient(string token)
        {
            _token = (token ?? "").Trim();
            _http = new HttpClient { BaseAddress = new Uri(DocsBaseUrl) };
        }

        private HttpRequestMessage NewRequest(HttpMethod method, string url)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            return req;
        }

        #endregion

        #region Auth

        // auth_check — validates the access token via Google tokeninfo and returns granted scopes
        public async Task<Tuple<bool, string, string>> ValidateTokenAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_token)) return Fail("Token is empty.");

                var req = NewRequest(HttpMethod.Get, TokenInfoUrl);
                var res = await _http.SendAsync(req).ConfigureAwait(false);
                string body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!res.IsSuccessStatusCode)
                    return Fail("HTTP " + (int)res.StatusCode + " " + res.ReasonPhrase + "\n" + body);

                return Ok(body);
            }
            catch (Exception ex) { return Fail(ex.ToString()); }
        }

        #endregion

        #region Document methods

        // find_documents_by_name — GET (Drive) /drive/v3/files?q=name contains '{name}' and mimeType=document
        // The Docs API cannot search; document discovery by name goes through the Drive API.
        // Returns the raw Drive files.list JSON ({ "files": [ { "id", "name" } ] }) for the component to parse.
        // NOTE: requires a Drive-scoped token (drive.readonly / drive.metadata.readonly); a documents-only token 401/403s.
        public async Task<Tuple<bool, string, string>> FindDocumentsByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return Fail("Name is empty.");

                // Escape single quotes for the Drive query language ( ' -> \' )
                string safeName = name.Replace("'", "\\'");
                string query = "name contains '" + safeName + "' and mimeType='application/vnd.google-apps.document' and trashed=false";

                string url = DriveFilesUrl
                    + "?q=" + Uri.EscapeDataString(query)
                    + "&pageSize=20"
                    + "&orderBy=" + Uri.EscapeDataString("modifiedTime desc")
                    + "&fields=" + Uri.EscapeDataString("files(id,name)");

                var req = NewRequest(HttpMethod.Get, url);
                var res = await _http.SendAsync(req).ConfigureAwait(false);
                string body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!res.IsSuccessStatusCode)
                    return Fail("HTTP " + (int)res.StatusCode + " " + res.ReasonPhrase + "\n" + body);

                return Ok(body);
            }
            catch (Exception ex) { return Fail(ex.ToString()); }
        }

        // get_document — GET /v1/documents/{documentId}
        public async Task<Tuple<bool, string, string>> GetDocumentAsync(string documentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(documentId)) return Fail("Document ID is empty.");

                var req = NewRequest(HttpMethod.Get, "/v1/documents/" + Uri.EscapeDataString(documentId));
                var res = await _http.SendAsync(req).ConfigureAwait(false);
                string body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!res.IsSuccessStatusCode)
                    return Fail("HTTP " + (int)res.StatusCode + " " + res.ReasonPhrase + "\n" + body);

                return Ok(body);
            }
            catch (Exception ex) { return Fail(ex.ToString()); }
        }

        // create_document — POST /v1/documents  body { title }
        public async Task<Tuple<bool, string, string>> CreateDocumentAsync(string title)
        {
            try
            {
                var payload = new JObject { ["title"] = title ?? "" };

                var req = NewRequest(HttpMethod.Post, "/v1/documents");
                req.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

                var res = await _http.SendAsync(req).ConfigureAwait(false);
                string body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!res.IsSuccessStatusCode)
                    return Fail("HTTP " + (int)res.StatusCode + " " + res.ReasonPhrase + "\n" + body);

                return Ok(body);
            }
            catch (Exception ex) { return Fail(ex.ToString()); }
        }

        // batch_update_document — POST /v1/documents/{documentId}:batchUpdate  body { requests: [...] }
        // requestsArrayJson is the serialised JSON array of Request objects (from RequestAggregator)
        public async Task<Tuple<bool, string, string>> BatchUpdateDocumentAsync(string documentId, string requestsArrayJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(documentId)) return Fail("Document ID is empty.");

                JArray requests;
                try
                {
                    requests = string.IsNullOrWhiteSpace(requestsArrayJson)
                        ? new JArray()
                        : JArray.Parse(requestsArrayJson);
                }
                catch (Exception ex) { return Fail("Requests JSON is not a valid JSON array: " + ex.Message); }

                if (requests.Count == 0) return Fail("No requests to apply — Requests JSON is empty.");

                var payload = new JObject { ["requests"] = requests };

                string url = "/v1/documents/" + Uri.EscapeDataString(documentId) + ":batchUpdate";
                var req = NewRequest(HttpMethod.Post, url);
                req.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

                var res = await _http.SendAsync(req).ConfigureAwait(false);
                string body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!res.IsSuccessStatusCode)
                    return Fail("HTTP " + (int)res.StatusCode + " " + res.ReasonPhrase + "\n" + body);

                return Ok(body);
            }
            catch (Exception ex) { return Fail(ex.ToString()); }
        }

        #endregion

        #region Image hosting (Drive)

        // upload_image_to_drive + set_file_public, chained — Docs API's insertInlineImage fetches its
        // "uri" unauthenticated, so a Bitmap or local File Path has to be hosted somewhere public first.
        // Uploads the bytes as a new Drive file, makes it link-public, and returns the URL form Docs can
        // fetch. Returns the URL as Item2 on success (matching the Ok/Fail shape of every other method).
        public async Task<Tuple<bool, string, string>> UploadImageAndGetUrlAsync(byte[] imageBytes, string fileName, string mimeType)
        {
            try
            {
                if (imageBytes == null || imageBytes.Length == 0) return Fail("Image data is empty.");

                string uploadUrl = DriveUploadUrl + "?uploadType=multipart&fields=" + Uri.EscapeDataString("id");
                var metadata = new JObject { ["name"] = string.IsNullOrWhiteSpace(fileName) ? "image.png" : fileName, ["mimeType"] = mimeType };

                using (var content = new MultipartContent("related"))
                {
                    var metadataPart = new StringContent(metadata.ToString(Formatting.None), Encoding.UTF8, "application/json");
                    var mediaPart = new ByteArrayContent(imageBytes);
                    mediaPart.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                    content.Add(metadataPart);
                    content.Add(mediaPart);

                    var uploadReq = NewRequest(HttpMethod.Post, uploadUrl);
                    uploadReq.Content = content;

                    var uploadRes = await _http.SendAsync(uploadReq).ConfigureAwait(false);
                    string uploadBody = await uploadRes.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!uploadRes.IsSuccessStatusCode)
                        return Fail("HTTP " + (int)uploadRes.StatusCode + " " + uploadRes.ReasonPhrase + "\n" + uploadBody);

                    string fileId = (string)JObject.Parse(uploadBody)["id"];
                    if (string.IsNullOrWhiteSpace(fileId)) return Fail("Upload succeeded but response had no file id:\n" + uploadBody);

                    var permReq = NewRequest(HttpMethod.Post, DriveFilesUrl + "/" + Uri.EscapeDataString(fileId) + "/permissions");
                    var permPayload = new JObject { ["role"] = "reader", ["type"] = "anyone" };
                    permReq.Content = new StringContent(permPayload.ToString(Formatting.None), Encoding.UTF8, "application/json");

                    var permRes = await _http.SendAsync(permReq).ConfigureAwait(false);
                    string permBody = await permRes.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!permRes.IsSuccessStatusCode)
                        return Fail("Uploaded (id=" + fileId + ") but could not make it link-public: HTTP " + (int)permRes.StatusCode + " " + permRes.ReasonPhrase + "\n" + permBody);

                    return Ok("https://drive.google.com/uc?id=" + fileId);
                }
            }
            catch (Exception ex) { return Fail(ex.ToString()); }
        }

        #endregion

        #region Private helpers

        private static Tuple<bool, string, string> Ok(string body)
            => new Tuple<bool, string, string>(true, body, "");

        private static Tuple<bool, string, string> Fail(string error)
            => new Tuple<bool, string, string>(false, "", error);

        #endregion
    }
}
