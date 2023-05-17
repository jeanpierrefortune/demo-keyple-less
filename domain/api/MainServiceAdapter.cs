using DemoKeypleLess.domain.data;
using DemoKeypleLess.domain.spi;
using DemoKeypleLess.domain.utils;
using DemoKeypleLess.infrastructure.pcscreader;
using DemoKeypleLess.infrastructure.server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using PCSC.Iso7816;
using Serilog;

namespace DemoKeypleLess.domain.api {
    class InputData {
    }

    class ExecuteRemoteServiceBodyContent {
        [JsonProperty ( "serviceId" )]
        public string ServiceId { get; set; }

        [JsonProperty ( "inputData" )]
        public InputData InputData { get; set; }
    }

    class MainServiceAdapter : MainServiceApi {
        private readonly ILogger _logger;
        private readonly ReaderServiceSpi _readerService;
        private readonly ServerSpi _server;
        private readonly string _clientNodeId;
        private string _localReaderName;

        private const int SW_6100 = 0x6100;
        private const int SW_6C00 = 0x6C00;
        private const int SW1_MASK = 0xFF00;
        private const int SW2_MASK = 0x00FF;

        internal MainServiceAdapter ( ReaderServiceSpi readerService, ServerSpi server )
        {
            _logger = Log.ForContext<MainServiceAdapter> ();

            _logger.Information ( "Creation of main service..." );

            _readerService = readerService;
            _server = server;

            _clientNodeId = Guid.NewGuid ().ToString ();

            List<string> readerNames = readerService.GetReaders ();
            if (readerNames.Count == 0)
            {
                _logger.Warning ( "No reader found!" );
                Environment.Exit ( 1 );
            }
            _localReaderName = readerNames[0];
            _logger.Information ( $"Select reader {_localReaderName}" );
            readerService.SelectReader ( _localReaderName );
        }


        private bool IsCase4 ( byte[] apduCommand )
        {
            if (apduCommand != null && apduCommand.Length > 4)
            {
                return apduCommand[4] == apduCommand.Length - 6;
            }
            return false;
        }


        ApduResponse ProcessApduRequest ( ApduRequest apduRequest )
        {
            ApduResponse apduResponse = new ApduResponse ();

            apduResponse.Apdu = _readerService.TransmitApdu ( apduRequest.Apdu );
            apduResponse.StatusWord = (apduResponse.Apdu[apduResponse.Apdu.Length - 2] << 8) | apduResponse.Apdu[apduResponse.Apdu.Length - 1];

            if (apduResponse.Apdu.Length == 2)
            {
                if ((apduResponse.StatusWord & SW1_MASK) == SW_6100)
                {
                    byte[] getResponseApdu = {
                        0x00,
                        0xC0,
                        0x00,
                        0x00,
                        (byte)(apduResponse.StatusWord & SW2_MASK)
                        };
                    apduResponse = ProcessApduRequest ( new ApduRequest { Apdu = getResponseApdu, Info = "Internal Get Response" } );
                }
                else if ((apduResponse.StatusWord & SW1_MASK) == SW_6C00)
                {
                    apduRequest.Apdu[apduRequest.Apdu.Length - 1] =
                        (byte)(apduResponse.StatusWord & SW2_MASK);
                    apduResponse = ProcessApduRequest ( apduRequest );
                }
                else if (IsCase4 ( apduRequest.Apdu )
                    && apduRequest.SuccessfulStatusWords.Contains ( apduResponse.StatusWord ))
                {
                    byte[] getResponseApdu = {
                    0x00,
                    0xC0,
                    0x00,
                    0x00,
                    apduRequest.Apdu[apduRequest.Apdu.Length - 1]
                    };
                    apduResponse = ProcessApduRequest ( new ApduRequest { Apdu = getResponseApdu, Info = "Internal Get Response" } );
                }
            }

            return apduResponse;
        }

        private byte ComputeSelectApplicationP2 ( FileOccurrence fileOccurrence, FileControlInformation fileControlInformation )
        {
            byte p2;

            switch (fileOccurrence)
            {
                case FileOccurrence.FIRST:
                    p2 = 0x00;
                    break;
                case FileOccurrence.LAST:
                    p2 = 0x01;
                    break;
                case FileOccurrence.NEXT:
                    p2 = 0x02;
                    break;
                case FileOccurrence.PREVIOUS:
                    p2 = 0x03;
                    break;
                default:
                    throw new Exception ( "Unexpected value: " + fileOccurrence );
            }

            switch (fileControlInformation)
            {
                case FileControlInformation.FCI:
                    p2 |= 0x00;
                    break;
                case FileControlInformation.FCP:
                    p2 |= 0x04;
                    break;
                case FileControlInformation.FMD:
                    p2 |= 0x08;
                    break;
                case FileControlInformation.NO_RESPONSE:
                    p2 |= 0x0C;
                    break;
                default:
                    throw new Exception ( "Unexpected value: " + fileControlInformation );
            }

            return p2;
        }

        private ApduResponse SelectApplication ( CardSelector cardSelector )
        {
            byte[] selectApplicationCommand = new byte[6 + cardSelector.Aid.Length];
            selectApplicationCommand[0] = 0x00; // CLA
            selectApplicationCommand[1] = 0xA4; // INS
            selectApplicationCommand[2] = 0x04; // P1: select by name
                                                // P2: b0,b1 define the File occurrence, b2,b3 define the File control information
                                                // we use the bitmask defined in the respective enums
            selectApplicationCommand[3] =
                ComputeSelectApplicationP2 (
                    cardSelector.FileOccurrence, cardSelector.FileControlInformation );
            selectApplicationCommand[4] = (byte)(cardSelector.Aid.Length); // Lc
            Array.Copy ( cardSelector.Aid, 0, selectApplicationCommand, 5, cardSelector.Aid.Length ); // data
            selectApplicationCommand[5 + cardSelector.Aid.Length] = 0x00; // Le

            ApduRequest apduRequest = new ApduRequest
            {
                Apdu = selectApplicationCommand
            };

            if (_logger.IsEnabled ( Serilog.Events.LogEventLevel.Debug ))
            {
                apduRequest.Info = "Internal Select Application";
            }

            return ProcessApduRequest ( apduRequest );
        }

        private CardResponse ProcessCardRequest ( CardRequest cardRequest )
        {
            var apduResponses = new List<ApduResponse> ();

            foreach (var apduRequest in cardRequest.ApduRequests)
            {
                try
                {
                    var apduResponse = ProcessApduRequest ( apduRequest );
                    apduResponses.Add ( apduResponse );

                    if (!apduRequest.SuccessfulStatusWords.Contains ( apduResponse.StatusWord ))
                    {
                        throw new UnexpectedStatusWordException ( "Unexpected status word." );
                    }
                }
                catch (ReaderIOException ex)
                {
                    // The process has been interrupted. We close the logical channel and throw a
                    // ReaderBrokenCommunicationException.
                    _readerService.ClosePhysicalChannel ();

                    throw new ReaderIOException ( "Reader communication failure while transmitting a card request.",
                        ex );
                }
                catch (UnexpectedStatusWordException ex)
                {
                    // The process has been interrupted. We close the logical channel and throw a
                    // CardBrokenCommunicationException.
                    _readerService.ClosePhysicalChannel ();

                    throw new CardIOException (
                        "Card communication failure while transmitting a card request.",
                        ex );
                }
            }

            return new CardResponse { IsLogicalChannelOpen = true, ApduResponses = apduResponses };
        }


        private CardSelectionResponse ProcessCardSelectionRequest ( CardSelectionRequest cardSelectionRequest )
        {
            ApduResponse selectAppResponse = SelectApplication ( cardSelectionRequest.CardSelector );
            CardResponse cardResponse = ProcessCardRequest ( cardSelectionRequest.CardRequest );
            CardSelectionResponse cardSelectionResponse = new CardSelectionResponse { HasMatched = true, PowerOnData = _readerService.GetPowerOnData (), SelectApplicationResponse = selectAppResponse, CardResponse = cardResponse };
            return cardSelectionResponse;
        }

        private MessageDto ProcessTransaction ( MessageDto message )
        {
            bool isServiceEnded = false;
            do
            {
                _logger.Information ( $"Processing action {message.Action}" );
                switch (message.Action)
                {
                    case "CMD":
                        var jsonObject = JObject.Parse ( message.Body );
                        string service = jsonObject["service"].ToString ();
                        JObject body = new JObject ();
                        body["service"] = service;
                        _logger.Information ( $"Service: {service}" );
                        switch (service)
                        {
                            case "IS_CONTACTLESS":
                                body["result"] = true;
                                break;
                            case "IS_CARD_PRESENT":
                                body["result"] = true;
                                break;
                            case "TRANSMIT_CARD_SELECTION_REQUESTS":
                                var transmitCardSelectionRequestsCmdBody = JsonConvert.DeserializeObject<TransmitCardSelectionRequestsCmdBody> ( message.Body );
                                var cardSelectionRequest = transmitCardSelectionRequestsCmdBody.Parameters.CardSelectionRequests[0];
                                var cardSelectionResponse = ProcessCardSelectionRequest ( cardSelectionRequest );
                                var cardSelectionResponses = new List<CardSelectionResponse> ();
                                cardSelectionResponses.Add ( cardSelectionResponse );
                                body["result"] = JArray.FromObject ( cardSelectionResponses );
                                break;
                            case "TRANSMIT_CARD_REQUESTS":
                                var transmitCardRequestsCmdBody = JsonConvert.DeserializeObject<TransmitCardRequestCmdBody> ( message.Body );
                                var cardRequest = transmitCardRequestsCmdBody.Parameters.CardRequest;
                                var cardResponse = ProcessCardRequest ( cardRequest );
                                body["result"] = JObject.FromObject ( cardResponse );
                                break;
                        }
                        message.SetAction ( "RESP" );
                        string jsonBodyString = JsonConvert.SerializeObject ( body, Formatting.None );
                        message.SetBody ( jsonBodyString ); 
                        var jsonResponse = _server.transmitRequest ( JsonConvert.SerializeObject ( message, Formatting.None ) );
                        message = JsonConvert.DeserializeObject<List<MessageDto>> ( jsonResponse )[0];
                        break;
                    case "END_REMOTE_SERVICE":
                        isServiceEnded = true;
                        break;
                    default:
                        _logger.Error ( "Unexpected action ID" );
                        isServiceEnded = true;
                        break;
                }

            } while (!isServiceEnded);
            return message;
        }

        private string ExecuteRemoteService ( string serviceId, string parameter )
        {
            string sessionId = Guid.NewGuid ().ToString ();

            // Create and fill InputData object
            InputData inputData = new InputData
            {

            };

            // Create and fill ExecuteRemoteServiceBodyContent object
            ExecuteRemoteServiceBodyContent bodyContent = new ExecuteRemoteServiceBodyContent
            {
                ServiceId = serviceId,
                InputData = inputData
            };

            // Create and fill RemoteServiceDto object


            var message = new MessageDto ()
                .SetAction ( "EXECUTE_REMOTE_SERVICE" )
                .SetBody ( JsonConvert.SerializeObject ( bodyContent, Formatting.None ) )
                .SetClientNodeId ( _clientNodeId )
                .SetLocalReaderName ( _localReaderName )
                .SetSessionId ( sessionId );

            var jsonResponse = _server.transmitRequest ( JsonConvert.SerializeObject ( message ) );

            message = JsonConvert.DeserializeObject<List<MessageDto>> ( jsonResponse )[0];

            message = ProcessTransaction ( message );

            return message.Body;
        }

        public string SelectAndReadContracts ( )
        {
            Guid sessionId = Guid.NewGuid ();
            _logger.Information ( "Waiting for a card..." );
            _readerService.WaitForCardPresent ();

            _logger.Information ( "Card found." );

            _readerService.OpenPhysicalChannel ();


            _logger.Information ( "Execute remote service." );

            return ExecuteRemoteService ( "SELECT_APP_AND_READ_CONTRACTS", "" );
        }

        public string SelectAndWriteContract ( int contractNumber )
        {
            throw new NotImplementedException ();
        }

        private const string GET_CHALLENGE = "0084000008";
        private const string SELECT_APP_IDFM = "00A404000AA000000404012509010100";

        /* 
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
                        catch (UnexpectedStatusWordException e) { Console.WriteLine ( $"A card IO exception occurred: {e.Message}" ); }
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

                    // Create and fill ExecuteRemoteServiceBodyContent object
                    ExecuteRemoteServiceBodyContent bodyContent = new ExecuteRemoteServiceBodyContent
                    {
                        ServiceId = "SELECT_APP_AND_READ_CONTRACTS",
                        InputData = inputData
                    };

                    // Create and fill RemoteServiceDto object
                    MessageDto serviceRequestDto = new MessageDto
                    {
                        Action = "EXECUTE_REMOTE_SERVICE",
                        Body = JsonConvert.SerializeObject ( bodyContent ),
                        ClientNodeId = clientNodeId.ToString (),
                        LocalReaderName = "READER_1",
                        SessionId = sessionId.ToString ()
                    };
                }*/
    }
}
