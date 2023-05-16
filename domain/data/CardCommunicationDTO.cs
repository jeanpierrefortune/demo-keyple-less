using DemoKeypleLess.domain.utils;
using Newtonsoft.Json;

namespace DemoKeypleLess.domain.data {
    public enum FileOccurrence {
        FIRST,
        LAST,
        NEXT,
        PREVIOUS,
    }

    public enum FileControlInformation {
        FCI,
        FCP,
        FMD,
        NO_RESPONSE,
    }
    public enum MultiSelectionProcessing {
        FIRST_MATCH,
        PROCESS_ALL,
    }

    public enum ChannelControl {
        KEEP_OPEN,
        CLOSE_AFTER,
    }

    public class CardSelector {
        [JsonConverter ( typeof ( HexStringByteArrayConverter ) )]
        public byte[] aid { get; set; }
        [JsonConverter ( typeof ( FileOccurrenceConverter ) )]
        public FileOccurrence fileOccurrence { get; set; }
        [JsonConverter ( typeof ( FileControlInformationConverter ) )]
        public FileControlInformation fileControlInformation { get; set; }
        [JsonConverter ( typeof ( HexStringSetToIntHashSetConverter ) )]
        public HashSet<int> successfulSelectionStatusWords { get; set; }
    }

    public class ApduRequest {
        [JsonConverter ( typeof ( HexStringByteArrayConverter ) )]
        public byte[] apdu { get; set; }
        public List<string> successfulStatusWords { get; set; }
        public string info { get; set; }
    }

    public class CardRequest {
        public List<ApduRequest> apduRequests { get; set; }
        public bool isStatusCodesVerificationEnabled { get; set; }
    }

    public class CardSelectionRequest {
        public CardSelector cardSelector { get; set; }
        public CardRequest cardRequest { get; set; }
    }

    public class TransmitCardSelectionRequestsParameters {
        public CardSelectionRequest[] cardSelectionRequests { get; set; }
        [JsonConverter ( typeof ( MultiSelectionProcessingConverter ) )]
        public MultiSelectionProcessing multiSelectionProcessing { get; set; }

        [JsonConverter ( typeof ( ChannelControlConverter ) )]
        public ChannelControl channelControl { get; set; }
    }

    public class TransmitCardSelectionRequestsCmdBody {
        public string service { get; set; }
        public TransmitCardSelectionRequestsParameters parameters { get; set; }
    }
}
