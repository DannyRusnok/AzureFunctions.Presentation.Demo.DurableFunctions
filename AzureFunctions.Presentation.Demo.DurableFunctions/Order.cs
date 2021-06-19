using System;

namespace AzureFunctions.Presentation.Demo.DurableFunctions
{
    public class Order
    {
        public int OrderId { get; set; }
        public string Email { get; set; }
        public int ProductId { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}