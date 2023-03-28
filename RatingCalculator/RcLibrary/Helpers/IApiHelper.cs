namespace RcLibrary.Helpers
{
    public interface IApiHelper
    {
        HttpClient? ApiClient { get; }
        bool IsInitialized { get; set; }

        Task<T> GetAsync<T>(string endpoint);
        void InitializeClient(string Url, int timeoutSeconds = 100);
    }
}