using System;
using System.Reflection;

namespace TinyMessenger {
    /// <summary>
    /// Represents an active subscription to a message
    /// </summary>
    public sealed class TinyMessageSubscriptionToken : IDisposable {
        private WeakReference _Hub;
        private Type _MessageType;

        /// <summary>
        /// Initializes a new instance of the TinyMessageSubscriptionToken class.
        /// </summary>
        public TinyMessageSubscriptionToken(ITinyMessengerHub hub, Type messageType) {
            if (hub == null)
                throw new ArgumentNullException("hub");

            _Hub = new WeakReference(hub);
            _MessageType = messageType;
        }

        public void Dispose() {
            if (_Hub.IsAlive) {
                var hub = _Hub.Target as ITinyMessengerHub;

                if (hub != null) {
                    var unsubscribeMethod = typeof(ITinyMessengerHub).GetTypeInfo().GetDeclaredMethod("Unsubscribe");
                    unsubscribeMethod = unsubscribeMethod.MakeGenericMethod(_MessageType);
                    unsubscribeMethod.Invoke(hub, new object[] { this });
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
