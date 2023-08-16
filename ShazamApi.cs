using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

static class ShazamApi {
    static readonly HttpClient HTTP = new HttpClient();
    static readonly string INSTALLATION_ID = Guid.NewGuid().ToString();

    static ShazamApi() {
        HTTP.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7");
    }

    public static async Task<ShazamResult> SendRequest(string tagId, int samplems, byte[] sig) {
        var payload = new {
            signature = new {
                uri = "data:audio/vnd.shazam.sig;base64," + Convert.ToBase64String(sig),
                samplems
            }
        };

        var url = "https://amp.shazam.com/discovery/v5/en/US/android/-/tag/" + INSTALLATION_ID + "/" + tagId;
        var postData = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var result = new ShazamResult();

        var res = await HTTP.PostAsync(url, postData);
        var obj = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync());

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
