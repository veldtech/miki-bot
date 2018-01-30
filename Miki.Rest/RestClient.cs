using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Rest
{
    public class RestClient
    {
        private HttpClient client;
		private List<Func<List<object>, Task>> preProcessors = new List<Func<List<object>, Task>>();
		private List<Func<HttpResponseMessage, List<object>, Task>> postProcessors = new List<Func<HttpResponseMessage, List<object>, Task>>();

        public RestClient(string base_address)
        {
            client = new HttpClient();
			client.BaseAddress = new Uri(base_address);
		}

        public RestClient AddHeader(string name, string value)
        {
            client.DefaultRequestHeaders.Add(name, value);
            return this;
        }
		public RestClient AddPreprocessor(Func<List<object>, Task> func)
		{
			preProcessors.Add(func);
			return this;
		}
		public RestClient AddPostprocessor(Func<HttpResponseMessage, List<object>, Task> func)
		{
			postProcessors.Add(func);
			return this;
		}

		public RestClient SetAuthorisation(string scheme, string value)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, value);
            return this;
        }
				
        public async Task<RestResponse<string>> GetAsync(string url)
        {
			HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
            RestResponse<string> r = new RestResponse<string>();
            r.Success = response.IsSuccessStatusCode;
            r.Data = await response.Content.ReadAsStringAsync();
            return r;
        }

		public async Task<RestResponse<T>> GetAsync<T>(string url)
        {
			HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
            RestResponse<T> r = new RestResponse<T>();
            r.Success = response.IsSuccessStatusCode;
            string output = await response.Content.ReadAsStringAsync();
            r.Data = JsonConvert.DeserializeObject<T>(output);
            return r;
        }
		
        public async Task<RestResponse<T>> PostAsync<T>(string url, string value)
        {
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(value, Encoding.UTF8, "application/json"));
            RestResponse<T> r = new RestResponse<T>();
			r.httpResponseMessage = response;
            r.Success = response.IsSuccessStatusCode;
            r.Data = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
            return r;
        }

		public async Task<RestResponse<T>> PostAsync<T>(string url)
		{
			List<object> arguments = new List<object>();
			await RunPreprocessorsAsync(arguments);
			HttpResponseMessage response = await client.PostAsync(url, null);
			await RunPostProcessorsAsync(arguments, response);
			RestResponse<T> r = new RestResponse<T>();
			r.Success = response.IsSuccessStatusCode;
			string output = await response.Content.ReadAsStringAsync();
			r.Data = JsonConvert.DeserializeObject<T>(output);
			return r;
		}

		public async Task<RestResponse<T>> PatchAsync<T>(string url, string value)
		{
			var method = new HttpMethod("PATCH");
			var request = new HttpRequestMessage(method, url)
			{
				Content = new StringContent(value, Encoding.UTF8, "application/json")
			};

			var response = default(HttpResponseMessage); 
			// In case you want to set a timeout
																	//CancellationToken cancellationToken = new CancellationTokenSource(60).Token;

			try
			{
				response = await client.SendAsync(request);
				// If you want to use the timeout you set
				//response = await client.SendRequestAsync(request).AsTask(cancellationToken);
			}
			catch (TaskCanceledException e)
			{
				Console.WriteLine("ERROR: " + e.ToString());
			}

			RestResponse<T> r = new RestResponse<T>();
			r.httpResponseMessage = response;
			r.Success = response.IsSuccessStatusCode;
			r.Data = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
			return r;
		}

		// Todo: check if it actually works?
		public async Task<RestResponse<string>> PostMultipartAsync(params MultiformItem[] items)
		{
			using (var content =  new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
			{
				for (int i = 0; i < items.Length; i++)
				{
					if (items[i].FileName == null)
					{
						content.Add(items[i].Content, items[i].Name);
						continue;
					}
					content.Add(items[i].Content, items[i].Name, items[i].FileName);
				}

				using (var message = await client.PostAsync("", content))
				{
					var input = await message.Content.ReadAsStringAsync();
					RestResponse<string> response = new RestResponse<string>();
					response.Data = input;
					response.httpResponseMessage = message;
					return response;
				}
			}
		}

		private async Task RunPostProcessorsAsync(List<object> arguments, HttpResponseMessage response)
		{
			foreach (var proc in postProcessors)
			{
				await proc(response, arguments);
			}
		}

		private async Task RunPreprocessorsAsync(List<object> objects)
		{
			foreach(var proc in preProcessors)
			{
				await proc(objects);
			}
		}
    }

	public class MultiformItem
	{
		public HttpContent Content { get; set; }
		public string Name { get; set; }
		public string FileName { get; set; } = null;
	}

    public enum RestReturnType
    {
        NONE,
        JSON,
        XML
    }
}