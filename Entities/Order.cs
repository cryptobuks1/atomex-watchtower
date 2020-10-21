namespace Atomex.Entities
{
    public enum Side
    {
        Buy,
        Sell
    }

    public enum OrderType
    {
        Return,
        FillOrKill,
        ImmediateOrCancel,
        SolidFillOrKill
    }

    public enum OrderStatus
    {
        Pending,
        Placed,
        PartiallyFilled,
        Filled,
        Canceled,
        Rejected
    }
}