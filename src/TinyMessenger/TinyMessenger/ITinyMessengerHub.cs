using System;

namespace TinyMessenger {
    /// <summary>
    /// Messenger hub responsible for taking subscriptions/publications and delivering of messages.
    /// </summary>
    public interface ITinyMessengerHub {
        // Set the main thread proxy
        ITinyMessageProxy MainThreadTinyMessageProxy { get; set; }

        /// <summary>
        /// Subscribe an object to all events that it subscribes to with the [Subscribe] method attribute
        /// 
        /// </summary>
        /// <param name="listener">Object that is listening for events. The hub will keep a reference to this object until
        /// Unregister is called with this listener</param>
        void Register(object listener);

        /// <summary>
        /// Unsubscribe an object from all events that it was subscribed to via Register
        /// 
        /// Does not throw an exception if the object was not registered
        /// </summary>
        /// <param name="listener">An object that has been registered for events</param>
        void Unregister(object listener);

        /// <summary>
        /// Publish a message to any subscribers
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="message">Message to deliver</param>
        void Publish<TMessage>(TMessage message) where TMessage : class;

        /// <summary>
        /// Publish a message to any subscribers asynchronously
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="message">Message to deliver</param>
        void PublishAsync<TMessage>(TMessage message) where TMessage : class;
    }
}
