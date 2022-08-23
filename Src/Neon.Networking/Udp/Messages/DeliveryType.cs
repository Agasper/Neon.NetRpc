namespace Neon.Networking.Udp.Messages
{
    public enum DeliveryType : byte //MAX = 8 (3 bit)
    {
        Unreliable = 0,
        UnreliableSequenced = 1,
        ReliableUnordered = 2,
        ReliableOrdered = 3
    }
}
