using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

static class ShazamApi {
    static readonly HttpClient HTTP = new HttpClient();
    static readonly string INSTALLATION_ID = Guid.NewGuid().ToString();

    public static async Task<ShazamResult> SendRequest(string tagId, int samplems, byte[] sig) {
        var payload = new {
            signature = new {
                uri = "data:audio/vnd.shazam.sig;base64," + Convert.ToBase64String(sig),
                samplems
            }
        };

        var url = "https://amp.shazam.com/discovery/v5/en/US/android/-/tag/" + INSTALLATION_ID + "/" + tagId;
        var postData = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json"
        );

        var result = new ShazamResult();

        try {
            var res = await HTTP.PostAsync(url, postData);
            var obj = JsonConvert.DeserializeObject<JToken>(await res.Content.ReadAsStringAsync());
            var track = obj.Value<JToken>("track");

            if(track != null) {
                result.Success = true;
                result.Url = track.Value<string>("url");
                result.Title = track.Value<string>("title");
                result.Artist = track.Value<string>("subtitle");
            } else {
                result.RetryMs = obj.Value<int>("retryms");
            }
        } catch {
        }

        return result;
    }

}
