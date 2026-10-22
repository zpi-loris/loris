using Protocol.V1;

namespace app.Messages
{
    public record DeviceMessage
    {
        public DateTime DateTime { get; init; }
        public required Message Payload { get; init; }
        public required byte[] DevEUI
        {
            get;
            init
            {
                field =
                    (value.Length == 8)
                        ? value
                        : throw new ArgumentException("DevEUI must be 8 bytes long", nameof(value));
            }
        }

        public void DoSomething()
        {
            switch (Payload.Type)
            {
                case MessageType.Capabilites:
                    Console.WriteLine("This is a Capabilites");
                    break;
                case MessageType.Hello:
                    Console.WriteLine("This is a Hello");
                    break;
                case MessageType.Report:
                    Console.WriteLine("This is a Report");
                    break;
                // Te poniżej nie powinny się wydarzyć
                case MessageType.Call:
                case MessageType.Unspecified:
                default:
                    throw new ArgumentException("Dupa, coś nie pykło");
            }
        }
    }
}
