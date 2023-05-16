using DemoKeypleLess.domain.data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoKeypleLess.domain.utils {
    public class HexStringByteArrayConverter : JsonConverter {
        public override bool CanConvert ( Type objectType )
        {
            return objectType == typeof ( byte[] );
        }

        public override object ReadJson ( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            var hexString = (string)reader.Value;
            return StringToByteArray ( hexString );
        }

        public override void WriteJson ( JsonWriter writer, object value, JsonSerializer serializer )
        {
            byte[] bytes = (byte[])value;
            writer.WriteValue ( BitConverter.ToString ( bytes ).Replace ( "-", "" ) );
        }

        public static byte[] StringToByteArray ( string hex )
        {
            return HexUtil.ToByteArray(hex);
        }
    }
}

public class HexStringSetToIntHashSetConverter : JsonConverter {
    public override bool CanConvert ( Type objectType )
    {
        return objectType == typeof ( HashSet<int> );
    }

    public override object ReadJson ( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
    {
        var hexStrings = serializer.Deserialize<List<string>> ( reader );
        return new HashSet<int> ( hexStrings.Select ( s => Convert.ToInt32 ( s, 16 ) ) );
    }

    public override void WriteJson ( JsonWriter writer, object value, JsonSerializer serializer )
    {
        var ints = (HashSet<int>)value;
        var hexStrings = ints.Select ( i => i.ToString ( "X4" ) ).ToList ();
        serializer.Serialize ( writer, hexStrings );
    }
}


public class FileOccurrenceConverter : JsonConverter {
    public override bool CanConvert ( Type objectType )
    {
        return objectType == typeof ( FileOccurrence );
    }

    public override object ReadJson ( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
    {
        var str = (string)reader.Value;
        return Enum.Parse ( typeof ( FileOccurrence ), str, true );
    }

    public override void WriteJson ( JsonWriter writer, object value, JsonSerializer serializer )
    {
        writer.WriteValue ( value.ToString () );
    }
}

public class FileControlInformationConverter : JsonConverter {
    public override bool CanConvert ( Type objectType )
    {
        return objectType == typeof ( FileControlInformation );
    }

    public override object ReadJson ( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
    {
        var str = (string)reader.Value;
        return Enum.Parse ( typeof ( FileControlInformation ), str, true );
    }

    public override void WriteJson ( JsonWriter writer, object value, JsonSerializer serializer )
    {
        writer.WriteValue ( value.ToString () );
    }
}

public class MultiSelectionProcessingConverter : JsonConverter {
    public override bool CanConvert ( Type objectType )
    {
        return objectType == typeof ( MultiSelectionProcessing );
    }

    public override object ReadJson ( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
    {
        var str = (string)reader.Value;
        return Enum.Parse ( typeof ( MultiSelectionProcessing ), str, true );
    }

    public override void WriteJson ( JsonWriter writer, object value, JsonSerializer serializer )
    {
        writer.WriteValue ( value.ToString () );
    }
}

public class ChannelControlConverter : JsonConverter {
    public override bool CanConvert ( Type objectType )
    {
        return objectType == typeof ( ChannelControl );
    }

    public override object ReadJson ( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
    {
        var str = (string)reader.Value;
        return Enum.Parse ( typeof ( ChannelControl ), str, true );
    }

    public override void WriteJson ( JsonWriter writer, object value, JsonSerializer serializer )
    {
        writer.WriteValue ( value.ToString () );
    }
}
