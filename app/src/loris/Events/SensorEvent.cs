using Protocol.V1;

namespace app.Events
{
    public sealed record SensorEvent : IEvent
    {
        public required byte[] DevEUI { get; init; }
        public required int SensorID { get; init; }

        public bool Equals(IEvent? other)
        {
            if (other is IEvent valueOfOther)
            {
                return Equals(valueOfOther as SensorEvent);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(SensorEvent? other)
        {
            if (other is SensorEvent valueOfOther)
            {
                return DevEUI.Equals(valueOfOther.DevEUI) && SensorID == valueOfOther.SensorID;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            // TODO: check the hash of table
            return HashCode.Combine(DevEUI, SensorID);
        }
    }
}
