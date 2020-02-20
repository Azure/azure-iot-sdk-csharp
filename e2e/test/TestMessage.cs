namespace Microsoft.Azure.Devices.E2ETests
{
    public class TestMessage
    {
        public Message CloudMessage { get; set; }
        public Client.Message ClientMessage { get; set; }
        public string Payload { get; set; }
        public string P1Value { get; set; }
        public string MessageId => CloudMessage?.MessageId ?? ClientMessage?.MessageId;
    }
}
