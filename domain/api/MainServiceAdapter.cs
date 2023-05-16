using DemoKeypleLess.domain.data;
using DemoKeypleLess.domain.spi;
using DemoKeypleLess.domain.utils;
using DemoKeypleLess.infrastructure.pcscreader;
using DemoKeypleLess.infrastructure.server;
using Newtonsoft.Json;
using Serilog;

namespace DemoKeypleLess.domain.api
{
    class MainServiceAdapter : MainServiceApi {
        private readonly ILogger _logger;
        private readonly ReaderServiceSpi _readerService;
        private readonly ServerSpi _server;
        private readonly Guid _clientNodeId;
        private string _readerName;

        internal MainServiceAdapter (ReaderServiceSpi readerService, ServerSpi server)
        {
            _logger = Log.ForContext<MainServiceAdapter> ();

            _readerService = readerService;
            _server = server;
            
            _clientNodeId = Guid.NewGuid ();

            List<string> readerNames = readerService.GetReaders ();
            if (readerNames.Count == 0)
            {
                _logger.Warning ( "No reader found!" );
                Environment.Exit ( 1 );
            }
            _readerName = readerNames[ 0 ];
            readerService.SelectReader( _readerName );
        }

        private CardSelectionRequest GetCardSelectionRequest( Guid sessionId )
        {
            // Create and fill InputData object
            InputData inputData = new InputData
            {

            };

            // Create and fill BodyContent object
            BodyContent bodyContent = new BodyContent
            {
                ServiceId = "SELECT_APP_AND_READ_CONTRACTS",
                InputData = inputData
            };

            // Create and fill RemoteServiceDTO object
            RemoteServiceDTO serviceRequestDto = new RemoteServiceDTO
            {
                Action = "EXECUTE_REMOTE_SERVICE",
                Body = JsonConvert.SerializeObject ( bodyContent ),
                ClientNodeId = _clientNodeId.ToString (),
                LocalReaderName = _readerName,
                SessionId = sessionId.ToString ()
            };

            string response = _server.transmitRequest ( JsonConvert.SerializeObject ( serviceRequestDto ) );

            _logger.Information ( $"response={response}" );

            List<RemoteServiceDTO> serviceResponseDtoList = JsonConvert.DeserializeObject<List<RemoteServiceDTO>> ( response );

            string serviceResponseBody = serviceResponseDtoList[0].Body;

            TransmitCardSelectionRequestsCmdBody transmitCardSelectionRequestsCmdBody = JsonConvert.DeserializeObject<TransmitCardSelectionRequestsCmdBody> ( serviceResponseBody );

            CardSelectionRequest cardSelectionRequest = transmitCardSelectionRequestsCmdBody.parameters.cardSelectionRequests[0];

            return cardSelectionRequest;
        }

        public string SelectAndReadContracts ( )
        {

            _readerService.WaitForCardPresent ();

            _readerService.OpenPhysicalChannel ();

            Guid sessionId = Guid.NewGuid ();

            CardSelectionRequest cardSelectionRequest = GetCardSelectionRequest ( sessionId );

            return cardSelectionRequest.ToString();
        }

        public string SelectAndWriteContract ( int contractNumber )
        {
            throw new NotImplementedException ();
        }

        private const string GET_CHALLENGE = "0084000008";
        private const string SELECT_APP_IDFM = "00A404000AA000000404012509010100";

        public void StartPcsc ( )
        {
            ReaderServiceSpi cardService = PcscReaderServiceSpiProvider.getInstance ();
            List<string> readerNames = cardService.GetReaders ();
            if (readerNames.Count == 0)
            {
                _logger.Warning ( "No reader found!" );
                Environment.Exit ( 1 );
            }
            cardService.SelectReader ( readerNames[0] );
            while (true)
            {
                try
                {
                    Console.WriteLine ( "Waiting for a card..." );
                    cardService.WaitForCardPresent ();
                    cardService.OpenPhysicalChannel ();
                    Console.WriteLine ( $"Power on data = {cardService.GetPowerOnData ()}" );
                    byte[] response = cardService.TransmitApdu ( HexUtil.ToByteArray ( SELECT_APP_IDFM ) );
                    Console.WriteLine ( $"Select App IDFM = {HexUtil.ToHex ( response )}" );
                    response = cardService.TransmitApdu ( HexUtil.ToByteArray ( GET_CHALLENGE ) );
                    Console.WriteLine ( $"Challenge = {HexUtil.ToHex ( response )}" );
                    Console.WriteLine ( "Waiting for the card removal..." );
                    cardService.WaitForCardAbsent ();
                }
                catch (CardIOException e) { Console.WriteLine ( $"A card IO exception occurred: {e.Message}" ); }
                catch (ReaderIOException e) { Console.WriteLine ( $"A reader IO exception occurred: {e.Message}" ); }
            }
        }

        public void StartClient ( )
        {
            Guid clientNodeId = Guid.NewGuid ();
            Guid sessionId = Guid.NewGuid ();
            ServerSpi server = ServerSpiProvider.getInstance ( "http://localhost", 8080, "card/remote-plugin" );

            // Create and fill InputData object
            InputData inputData = new InputData
            {

            };

            // Create and fill BodyContent object
            BodyContent bodyContent = new BodyContent
            {
                ServiceId = "SELECT_APP_AND_READ_CONTRACTS",
                InputData = inputData
            };

            // Create and fill RemoteServiceDTO object
            RemoteServiceDTO serviceRequestDto = new RemoteServiceDTO
            {
                Action = "EXECUTE_REMOTE_SERVICE",
                Body = JsonConvert.SerializeObject ( bodyContent ),
                ClientNodeId = clientNodeId.ToString (),
                LocalReaderName = "READER_1",
                SessionId = sessionId.ToString ()
            };
        }
    }
}
