using System;

namespace TinyMessenger {
    internal class StrongTinyMessageSubscription<TMessage> : ITinyMessageSubscription
        where TMessage : class {
        protected TinyMessageSubscriptionToken _SubscriptionToken;
        protected Action<TMessage> _DeliveryAction;
        protected Func<TMessage, bool> _MessageFilter;

        public TinyMessageSubscriptionToken SubscriptionToken {
            get { return _SubscriptionToken; }
        }

        public bool ShouldAttemptDelivery(object message) {
            if (!(message is TMessage))
                return false;

            return _MessageFilter.Invoke(message as TMessage);
        }

        public void Deliver(object message) {
            if (!(message is TMessage))
                throw new ArgumentException("Message is not the correct type");

            _DeliveryAction.Invoke(message as TMessage);
        }

        /// <summary>
        /// Initializes a new instance of the TinyMessageSubscription class.
        /// </summary>
        /// <param name="destination">Destination object</param>
        /// <param name="deliveryAction">Delivery action</param>
        /// <param name="messageFilter">Filter function</param>
        public StrongTinyMessageSubscription(TinyMessageSubscriptionToken subscriptionToken, Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter) {
            if (subscriptionToken == null)
                throw new ArgumentNullException("subscriptionToken");

            if (deliveryAction == null)
                throw new ArgumentNullException("deliveryAction");

            if (messageFilter == null)
                throw new ArgumentNullException("messageFilter");

            _SubscriptionToken = subscriptionToken;
            _DeliveryAction = deliveryAction;
            _MessageFilter = messageFilter;
        }
    }
}