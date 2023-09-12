using System.Net.Http.Headers;

namespace RcLibrary.Helpers;

public class ApiHelper : IApiHelper
{
    private HttpClient? _apiClient;
    public bool IsInitialized { get; set; } = false;
    public HttpClient? ApiClient { get { return _apiClient; } }

    public void InitializeClient(string Url, int timeoutSeconds = 100)
    {
        _apiClient = new HttpClient { BaseAddress = new Uri(Url), Timeout = new TimeSpan(0, 2, 0) };
        _apiClient.Timeout = new TimeSpan(0, 0, timeoutSeconds);
        _apiClient.DefaultRequestHeaders.Accept.Clear();
        _apiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        IsInitialized = true;
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        try
        {
            if (ApiClient == null) { throw new Exception("Not initialized"); }
            using HttpResponseMessage response = await ApiClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<T>();
                return result;
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
        }
        catch (Exception) { throw; }
    }
}
