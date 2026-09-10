namespace RetailInventoryApp.Models;

public enum StockMovementType
{
    StockIn,
    StockOut,
    Adjustment,
    Sale
}

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public StockMovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; } = DateTime.Now;
}
