using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeoVR.QuicNet.Haptics
{
    public class HapticApi : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _deviceId;

        public HapticApi(string baseUrl, string deviceId)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _deviceId = deviceId;
        }

        public void Dispose() => ((IDisposable)_httpClient).Dispose();

        public async Task<List<Publication>> GetPublications()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/haptics/publishers");
            var response = await _httpClient.SendAsync(request);
            var data = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GetPublicationsResponse>(data);
            return result.publications;
        }

        public async Task<AuthorizeResponse> AuthSubsciber(string publicationId, string subscriberId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/haptics/publishers/{publicationId}/subscribe/{subscriberId}");
            request.Content = JsonContent.Create(new AuthorizeRequest { device_id = _deviceId });
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<AuthorizeResponse>();
        }

        public class GetPublicationsResponse
        {
            public List<Publication> publications { get; set; }
        }

        public class Publication
        {
            public string publication_id { get; set; }
            public string device_id { get; set; }
        }

        public class AuthorizeRequest
        {
            public string device_id { get; set; }
        }

        public class AuthorizeResponse
        {
            public string jwt_key { get; set; }
            public string subscription_id { get; set; }
        }
    }
}
