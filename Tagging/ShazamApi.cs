using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Project;

static class ShazamApi {
    static readonly HttpClient HTTP = new HttpClient();
    static readonly string INSTALLATION_ID = Guid.NewGuid().ToString();

    static ShazamApi() {
        HTTP.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7");
    }

    public static async Task<ShazamResult> SendRequestAsync(string tagId, int samplems, byte[] sig) {
        using var payloadStream = new MemoryStream();
        using var payloadWriter = new Utf8JsonWriter(payloadStream);

        payloadWriter.WriteStartObject();
        payloadWriter.WritePropertyName("signatures");
        payloadWriter.WriteStartArray();
        payloadWriter.WriteStartObject();
        payloadWriter.WriteString("uri", "data:audio/vnd.shazam.sig;base64," + Convert.ToBase64String(sig));
        payloadWriter.WriteNumber("samplems", samplems);
        payloadWriter.WriteEndObject();
        payloadWriter.WriteEndArray();
        payloadWriter.WriteString("timezone", "GMT");
        payloadWriter.WriteEndObject();
        payloadWriter.Flush();

        var url = "https://amp.shazam.com/match/v1/en/US/android/" + INSTALLATION_ID + "/" + tagId;
        var postData = new ByteArrayContent(payloadStream.GetBuffer(), 0, (int)payloadStream.Length);
        postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var result = new ShazamResult();

        var res = await HTTP.PostAsync(url, postData);
        var json = await res.Content.ReadAsByteArrayAsync();
        var obj = ParseJson(json);

        PopulateResult(obj, result);

        return result;
    }

    static JsonElement ParseJson(byte[] json) {
        var reader = new Utf8JsonReader(json.AsSpan());
        return JsonElement.ParseValue(ref reader);
    }

    static void PopulateResult(JsonElement rootElement, ShazamResult result) {
        if(!rootElement.TryGetProperty("results", out var resultsElement))
            return;

        PopulateID(resultsElement, result);

        if(!String.IsNullOrEmpty(result.ID)) {
            result.Success = true;
            PopulateAttributes(rootElement, result);
        } else {
            PopulateRetryMs(resultsElement, result);
        }
    }

    static void PopulateID(JsonElement resultsElement, ShazamResult result) {
        if(!resultsElement.TryGetProperty("matches", out var matchesElement))
            return;

        TryGetFirstItemID(matchesElement, out result.ID);
    }

    static void PopulateRetryMs(JsonElement resultsElement, ShazamResult result) {
        if(!TryGetNestedProperty(resultsElement, ["retry", "retryInMilliseconds"], out var retryMsElement))
            return;

        if(!retryMsElement.TryGetInt32(out var retryMs))
            return;

        result.RetryMs = retryMs;
    }

    static void PopulateAttributes(JsonElement rootElement, ShazamResult result) {
        if(!TryGetNestedProperty(rootElement, ["resources", "shazam-songs", result.ID], out var shazamSongElement))
            return;

        if(!shazamSongElement.TryGetProperty("attributes", out var attrsElement))
            return;

        if(attrsElement.TryGetProperty("title", out var titleElement))
            result.Title = titleElement.GetString();

        if(attrsElement.TryGetProperty("artist", out var artistElement))
            result.Artist = artistElement.GetString();

        if(attrsElement.TryGetProperty("webUrl", out var webUrlElement))
            result.Url = webUrlElement.GetString();

        if(!String.IsNullOrEmpty(result.Url)) {
            result.Url = ImproveUrl(result.Url);
        } else {
            result.Url = "https://www.shazam.com/track/" + result.ID;
        }

        PopulateAppleID(shazamSongElement, result);
    }

    static string ImproveUrl(string url) {
        var qsIndex = url.IndexOf('?');
        if(qsIndex > -1)
            url = url.Substring(0, qsIndex);

        // make slug readable
        url = Uri.UnescapeDataString(url);

        return url;
    }

    static void PopulateAppleID(JsonElement shazamSongElement, ShazamResult result) {
        if(!shazamSongElement.TryGetProperty("relationships", out var relsElement))
            return;

        if(TryGetNestedProperty(relsElement, ["songs", "data"], out var songsElement))
            TryGetFirstItemID(songsElement, out result.AppleSongID);
    }

    static bool TryGetFirstItemID(JsonElement array, out string id) {
        if(array.ValueKind == JsonValueKind.Array && array.GetArrayLength() > 0) {
            if(array[0].TryGetProperty("id", out var itemElement)) {
                id = itemElement.GetString();
                return true;
            }
        }
        id = default;
        return false;
    }

    static bool TryGetNestedProperty(JsonElement element, string[] names, out JsonElement value) {
        foreach(var name in names) {
            if(!element.TryGetProperty(name, out element)) {
                value = default;
                return false;
            }
        }
        value = element;
        return true;
    }
}
