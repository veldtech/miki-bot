using System.Net.Http;

namespace Rest
{
    public class RestResponse<T>
    {
        public bool Success { get; internal set; }
        public T Data { get; internal set; }
		public HttpResponseMessage httpResponseMessage { get; internal set; }
    }
}