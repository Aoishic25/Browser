using System;
using System.Net.Http;
using System.Threading.Tasks;
using F21SC_webbrowser;

namespace F21SC_webbrowser
{
    public class HttpFetcher
    {
        // Static HttpClient instance to be reused
        private static readonly HttpClient client = new HttpClient();

        // Asynchronous method to fetch HTML content from a URL
        public async Task<(int StatusCode, string ReasonPhrase, string Html)> FetchAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url); // Send GET request
                string html = await response.Content.ReadAsStringAsync(); // Read response content as string

                return ((int)response.StatusCode, response.ReasonPhrase, html); // Return status code, reason phrase, and HTML content
            }
            catch (HttpRequestException ex)
            {
                // Return custom message for errors like invalid URL
                return (0, $"Error: {ex.Message}", "");
            }
        }
    }
}
