using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bridge
{
    public class BridgeHttpListener
    {
        private HttpListener _listener = new HttpListener();
        private readonly ILogger<BridgeHttpListener> logger;
        private readonly IConfiguration configuration;

        private int port { get; }

        public BridgeHttpListener(
            ILogger<BridgeHttpListener> logger
            , IConfiguration configuration)
        {
            port = int.Parse(configuration["BridgeListenerPort"]!);
            this.logger = logger;
            this.configuration = configuration;
            StartListening();
        }

        internal void StartListening()
        {
            try
            {
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
                _listener.BeginGetContext(RequestCallback, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error opening listener");
            }
        }
        private void RequestCallback(IAsyncResult ar)
        {
            HttpListenerContext? context = null;
            try
            {
                if (_listener.IsListening)
                {
                    context = _listener.EndGetContext(ar);
                    HandleRequest(context);
                    _listener.BeginGetContext(RequestCallback, null);  //Enable new requests
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "The specified network name is no longer available.")
                {
                    logger.LogError(ex, "Error handeling listener request");
                    WriteStringToResponse(context, ex.ToString());
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            logger.LogInformation($"Got request {context.Request.Url}");
            var request = context.Request;
            if (request.Url != null)
            {
                if (request.Url.ToString().ToUpper().EndsWith("PING"))
                {
                    WriteStringToResponse(context, "Pong");
                }
                
            }
        }

        
        internal void StopListening()
        {
            _listener.Stop();
        }

        private void HandlePosRequest(HttpListenerContext context)
        {
            var requestData = GetRequestPostData(context.Request);
            string? responseMsg = null;

            if (requestData != null)
            {
                var req = JsonConvert.DeserializeObject<RequestDto>(requestData);

                

            }
            if (responseMsg != null)
            {
                WriteStringToResponse(context, responseMsg);
            }
        }

        private void WriteStringToResponse(HttpListenerContext? context, string responseMsg)
        {
            if (context != null)
            {
                try
                {
                    context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                    var responseMsgBytes = Encoding.UTF8.GetBytes(responseMsg);
                    context.Response.ContentLength64 = responseMsgBytes.Length;  //Response msg size
                    context.Response.OutputStream.Write(responseMsgBytes, 0, responseMsgBytes.Length);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "error writing response to handler request");
                }
                finally
                {
                    context.Response.OutputStream.Close();
                }
            }
            else
            {
                logger.LogError("context is null in handler request");
            }
        }

        private string? GetRequestPostData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return null;
            }
            using (System.IO.Stream body = request.InputStream) // here we have data
            {
                using (var reader = new System.IO.StreamReader(body, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        
        
        private string ChangeReferencesToSubPath(string s)
        {
            string responseString = s;

            responseString = responseString.Replace("src='/", "src='./");
            responseString = responseString.Replace("src=\"/", "src=\"./");
            responseString = responseString.Replace("href='/", "href='./");
            responseString = responseString.Replace("href=\"/", "href=\"./");
            return responseString;
        }

        private void HandleDefaultRequest(HttpListenerContext context, HttpResponseMessage result)
        {
            result.Content.ReadAsStream().CopyTo(context.Response.OutputStream);
            context.Response.OutputStream.Close();
        }
    }
}
