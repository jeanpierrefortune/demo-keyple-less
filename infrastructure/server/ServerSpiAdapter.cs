
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DemoKeypleLess.domain.spi;
using DemoKeypleLess.infrastructure.pcscreader;
using Newtonsoft.Json;
using Serilog;

namespace DemoKeypleLess.infrastructure.server {

    internal class ServerSpiAdapter : ServerSpi {
        private readonly ILogger _logger;
        private readonly string _baseUrl;
        private readonly string _endPoint;

        public ServerSpiAdapter ( string baseUrl, int port, string endPoint )
        {
            _logger = Log.ForContext<ServerSpiAdapter> ();
            _baseUrl = $"{baseUrl}:{port}";
            _endPoint = endPoint ;
        }

        public string transmitRequest ( string jsonRequest )
        {
            _logger.Information ( $"request = {jsonRequest}" );
            string result = null;
            try
            {
                using (var httpClient = new HttpClient { BaseAddress = new Uri ( _baseUrl ) })
                {
                    var content = new StringContent ( jsonRequest, Encoding.UTF8, "application/json" );
                    var response = httpClient.PostAsync ( _endPoint, content ).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        result = response.Content.ReadAsStringAsync ().Result;
                    }
                    else
                    {
                        Console.WriteLine ( $"Error when calling the API: {response.StatusCode}" );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine ( $"Exception when calling the API: {ex.Message}" );
            }

            return result;
        }
    }
}