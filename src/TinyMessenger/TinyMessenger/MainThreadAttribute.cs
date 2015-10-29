using System;

namespace TinyMessenger {
    /// <summary>
    /// Specify alongside the Subscribe attribute so that messages will be delivered to the method on the main thread.
    /// Client applications must specify the main-thread message proxy.
    /// 
    /// <example>
    /// public class ConsoleLogger
    /// {
    ///     [Subscribe, MainThread]
    ///     public void OnPurchase(IPurchaseEvent purchase)
    ///     {
    ///         Console.WriteLine(purchase.GetAmount() + " on main thread");
    ///     }
    /// }
    /// </example>
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class MainThread : Attribute {
    }
}
