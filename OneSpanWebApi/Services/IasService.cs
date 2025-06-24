using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OneSpanSign.Sdk;
using OneSpanWebApi.Data;
using OneSpanWebApi.Models;
using System.Configuration;
using System.Net;
using System.Text;

namespace OneSpanWebApi.Services
{
    public class IasService
    {
        private const string NniPath = "nni";
        private const string PingPath = "ping";
        private readonly ILogger<IasService> _logger;
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly string env;
        private readonly string lib;
        private readonly Uri nniUri;
        private readonly Uri pingUri;

        public IasService(IOptions<IasClientConfig> options, ILogger<IasService> logger)
        {
            var config = options.Value;
            env = config.Environment;
            lib = config.Library;
            nniUri = new Uri(config.Uri, NniPath);
            pingUri = new Uri(config.Uri, PingPath);
            _logger = logger;
        }

        public void NatServJCall(string processName, string requestData, out StringBuilder responseData)
        {
            responseData = new StringBuilder();
            responseData.Append(Query("NATSERVJ", processName.ToString(), requestData.ToString()));
        }

        public string Query(string programName, string functionName, string requestData)
        {
            return Task.Run(() => QueryAsync(programName, functionName, requestData)).Result;
        }

        /// <summary>
        /// Queries the Ias
        /// </summary>
        /// <param name="programName">Name of the Natural Program.</param>
        /// <param name="functionName">Name of the Natural Function.</param>
        /// <param name="requestData">The request data.</param>
        /// <returns>Return string data from Natural, can be null or empty</returns>
        public async Task<string> QueryAsync(string programName, string functionName, string requestData)
        {
            var formParameters = new Dictionary<string, string>
            {
                { "env", env },
                { "lib", lib },
                { "pgm", programName },
                { "func", functionName },
                { "data", requestData }
            };

            try
            {
                var encodedItems = formParameters.Select(i => WebUtility.UrlEncode(i.Key) + "=" + WebUtility.UrlEncode(i.Value));
                var encodedContent = new StringContent(string.Join("&", encodedItems), null, "application/x-www-form-urlencoded");

                _logger.LogInformation($"Request to {nniUri}: {encodedContent.ReadAsStringAsync().Result}");

                var response = await httpClient.PostAsync(nniUri, encodedContent);
                response.EnsureSuccessStatusCode();

                // Log the response status code
                _logger.LogInformation($"Response from {nniUri}: {response.StatusCode}");
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError($"Error querying Ias: {response.StatusCode}");
                    return string.Empty;
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Error querying Ias: {ex.Message}");
                return string.Empty;
            }
        }

        public StringBuilder DesignationStatusUpdate(int designationid)
        {
            StringBuilder response = new StringBuilder();

            JObject request = new JObject();
            request.Add("designationid", designationid);
            request.Add("status", "final");
            request.Add("signatureDate", DateTime.Now.ToString("MM-dd-yyyy"));

            _logger.LogInformation($"Request to finalize designation: {request.ToString()}");

            this.NatServJCall("FinalizeDesignation", request.ToString(), out response);

            _logger.LogInformation($"Response from FinalizeDesignation: {response.ToString()}");

            return response;
        }
    }
}
