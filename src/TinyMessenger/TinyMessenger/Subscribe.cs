using System;

namespace TinyMessenger {
    /// <summary>
    /// Subscribers that would like to declare a method as handler for a specific message, should add 
    /// this attribute to the method declaration.
    /// 
    /// <example>
    /// public class ConsoleLogger
    /// {
    ///     [Subscribe]
    ///     public void OnPurchase(IPurchaseEvent purchase)
    ///     {
    ///         Console.WriteLine(purchase.GetAmount());
    ///     }
    /// }
    /// </example>
    /// NB: The method must have only one parameter, and its type determines the event type.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class Subscribe : Attribute {
    }
}
