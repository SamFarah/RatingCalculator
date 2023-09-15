namespace RcLibrary.Helpers;

public interface IApiHelper
{
    bool IsInitialized { get; set; }

    void AddBasicAuthHeader(string authStr);
    void AddBearerAuthHeader(string token);
    void Dispose();
    Task<T> GetAsync<T>(string endpoint);
    void InitializeClient(string Url, int timeoutSeconds = 100);
    Task<T> PostAsync<T>(string endpoint, HttpContent content);
}