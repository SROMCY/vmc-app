using FileHelpers;

namespace VMC.Measurement
{
    [DelimitedRecord("=")]
    public class MetaData
    {
        public string Key { get; private set; }
        public string Value { get; private set; }

        public MetaData()
        {
            Key = "#";
            Value = string.Empty;
        }

        public MetaData(string key, string value)
        {
            Key = "#" + key;
            Value = value;
        }
    }
}
