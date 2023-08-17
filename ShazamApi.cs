using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        payloadWriter.WritePropertyName("signature");
        payloadWriter.WriteStartObject();
        payloadWriter.WriteString("uri", "data:audio/vnd.shazam.sig;base64," + Convert.ToBase64String(sig));
        payloadWriter.WriteNumber("samplems", samplems);
        payloadWriter.WriteEndObject();
        payloadWriter.WriteEndObject();
        payloadWriter.Flush();

        var url = "https://amp.shazam.com/discovery/v5/en/US/android/-/tag/" + INSTALLATION_ID + "/" + tagId;
        var postData = new ByteArrayContent(payloadStream.GetBuffer(), 0, (int)payloadStream.Length);
        postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var result = new ShazamResult();

        var res = await HTTP.PostAsync(url, postData);
        var json = await res.Content.ReadAsStringAsync();
        var obj = JsonSerializer.Deserialize(json, ShazamApiJsonSerializerContext.Default.JsonElement);

        if(obj.TryGetProperty("track", out var track)) {
            result.Success = true;
            result.Url = track.GetProperty("url").GetString();
            result.Title = track.GetProperty("title").GetString();
            result.Artist = track.GetProperty("subtitle").GetString();
        } else {
            if(obj.TryGetProperty("retryms", out var retryMs))
                result.RetryMs = retryMs.GetInt32();
        }

        return result;
    }

}

[JsonSerializable(typeof(JsonElement))]
partial class ShazamApiJsonSerializerContext : JsonSerializerContext {
}
