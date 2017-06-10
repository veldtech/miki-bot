using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Rest
{
    public class RestClient
    {
        HttpClient client;

        string baseUrl;

        public RestClient(string base_url)
        {
            baseUrl = base_url;
            client = new HttpClient();
            client.BaseAddress = new Uri(base_url);
        }

        public RestClient AddHeader(string name, string value)
        {
            client.DefaultRequestHeaders.Add(name, value);
            return this;
        }

        public RestClient SetAuthorisation(string scheme, string value)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, value);
            return this;
        }

        public RestClient AsJson()
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return this;
        }

        public async Task<RestResponse<string>> GetAsync()
        {
            HttpResponseMessage response = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
            RestResponse<string> r = new RestResponse<string>();
            r.Success = response.IsSuccessStatusCode;
            r.Data = await response.Content.ReadAsStringAsync();
            return r;
        }

        public async Task<RestResponse<T>> GetAsync<T>()
        {
            HttpResponseMessage response = await client.GetAsync("", HttpCompletionOption.ResponseContentRead);
            RestResponse<T> r = new RestResponse<T>();
            r.Success = response.IsSuccessStatusCode;
            string output = await response.Content.ReadAsStringAsync();
            r.Data = JsonConvert.DeserializeObject<T>(output);
            return r;
        }

        public async Task<RestResponse<string>> PostAsync()
        {
            HttpResponseMessage response = await client.PostAsync("", null);
            RestResponse<string> r = new RestResponse<string>();
            r.Success = response.IsSuccessStatusCode;
            r.Data = await response.Content.ReadAsStringAsync();
            return r;
        }

        public async Task<RestResponse<T>> PostAsAsync<T>()
        {
            HttpResponseMessage response = await client.PostAsync("", null);
            RestResponse<T> r = new RestResponse<T>();
            r.Success = response.IsSuccessStatusCode;
            string output = await response.Content.ReadAsStringAsync();
            r.Data = JsonConvert.DeserializeObject<T>(output);
            return r;
        }
    }

    public enum RestReturnType
    {
        NONE,
        JSON,
        XML
    }
}
