using Protocol.V1;

namespace app.Messages
{
    // public record SensorEventMessage : IEventMessage
    // {
    //     public required byte[] DevEUI { get; init; }
    //     public required int SensorID { get; init; }
    //
    //     public Report Previous { get; init; }
    //     public  Report Current { get; init; }
    //
    //     public bool Equals(IEventMessage? other)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public void DoSth()
    //     {
    //         switch (Previous.ValueCase)
    //         {
    //             case Report.ValueOneofCase.Bool:
    //                 break;
    //             case Report.ValueOneofCase.None:
    //                 break;
    //             case Report.ValueOneofCase.Float:
    //                 break;
    //             case Report.ValueOneofCase.String:
    //                 break;
    //             default:
    //                 Console.WriteLine("Do something with the default");
    //                 break;
    //         }
    //     }
    //
    //     public override int GetHashCode()
    //     {
    //         return HashCode.Combine(DevEUI, SensorID);
    //     }
    // }
    public record SensorEventMessage : BaseEventMessage
    {
        public required Report Previous { get; init; }
        public required Report Current { get; init; }
    }
}
