using System.Net.Http.Headers;

namespace RcLibrary.Helpers;

public class ApiHelper : IApiHelper
{
    private HttpClient? _apiClient;
    public bool IsInitialized { get; set; } = false;


    public void InitializeClient(string Url, int timeoutSeconds = 100)
    {
        _apiClient = new HttpClient { BaseAddress = new Uri(Url), Timeout = new TimeSpan(0, 2, 0) };
        _apiClient.Timeout = new TimeSpan(0, 0, timeoutSeconds);
        _apiClient.DefaultRequestHeaders.Accept.Clear();
        _apiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        IsInitialized = true;
    }

    public void AddBasicAuthHeader(string authStr)
    {
        if (_apiClient != null)
        {
            _apiClient.DefaultRequestHeaders.Accept.Clear();
            _apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authStr);
        }
    }

    public void AddBearerAuthHeader(string token)
    {
        if (_apiClient != null)
        {
            _apiClient.DefaultRequestHeaders.Accept.Clear();
            _apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        try
        {
            if (_apiClient == null) { throw new Exception("Not initialized"); }
            using HttpResponseMessage response = await _apiClient.GetAsync(endpoint);
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

    public async Task<T> PostAsync<T>(string endpoint, HttpContent content)
    {
        try
        {
            if (_apiClient == null) { throw new Exception("Not initialized"); }
            using HttpResponseMessage response = await _apiClient.PostAsync(endpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<T>();
                return result;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }
        }
        catch (Exception) { throw; }
    }

    public void Dispose()
    {
        _apiClient?.Dispose();
    }
}
