using System;

namespace TinyMessenger {
    internal class WeakTinyMessageSubscription<TMessage> : ITinyMessageSubscription
        where TMessage : class {
        protected TinyMessageSubscriptionToken _SubscriptionToken;
        protected WeakReference _DeliveryAction;
        protected WeakReference _MessageFilter;

        public TinyMessageSubscriptionToken SubscriptionToken {
            get { return _SubscriptionToken; }
        }

        public bool ShouldAttemptDelivery(object message) {
            if (!(message is TMessage))
                return false;

            if (!_DeliveryAction.IsAlive)
                return false;

            if (!_MessageFilter.IsAlive)
                return false;

            return ((Func<TMessage, bool>)_MessageFilter.Target).Invoke(message as TMessage);
        }

        public void Deliver(object message) {
            if (!(message is TMessage))
                throw new ArgumentException("Message is not the correct type");

            if (!_DeliveryAction.IsAlive)
                return;

            ((Action<TMessage>)_DeliveryAction.Target).Invoke(message as TMessage);
        }

        /// <summary>
        /// Initializes a new instance of the WeakTinyMessageSubscription class.
        /// </summary>
        /// <param name="destination">Destination object</param>
        /// <param name="deliveryAction">Delivery action</param>
        /// <param name="messageFilter">Filter function</param>
        public WeakTinyMessageSubscription(TinyMessageSubscriptionToken subscriptionToken, Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter) {
            if (subscriptionToken == null)
                throw new ArgumentNullException("subscriptionToken");

            if (deliveryAction == null)
                throw new ArgumentNullException("deliveryAction");

            if (messageFilter == null)
                throw new ArgumentNullException("messageFilter");

            _SubscriptionToken = subscriptionToken;
            _DeliveryAction = new WeakReference(deliveryAction);
            _MessageFilter = new WeakReference(messageFilter);
        }
    }
}

