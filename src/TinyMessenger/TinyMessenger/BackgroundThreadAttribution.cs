using System;

namespace TinyMessenger {
    /// <summary>
    /// Specify alongside the Subscribe attribute so that messages will be delivered to the method on a background thread.
    /// Client applications must specify the background-thread message proxy.
    /// 
    /// <example>
    /// public class ConsoleLogger
    /// {
    ///     [Subscribe, BackgroundThread]
    ///     public void OnPurchase(IPurchaseEvent purchase)
    ///     {
    ///         Console.WriteLine(purchase.GetAmount() + " on a background thread");
    ///     }
    /// }
    /// </example>
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class BackgroundThread : Attribute {
    }
}
