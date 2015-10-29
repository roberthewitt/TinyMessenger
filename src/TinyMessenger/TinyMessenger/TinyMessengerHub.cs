//===============================================================================
// TinyMessenger
//
// A simple messenger/event aggregator.
//
// https://github.com/grumpydev/TinyMessenger
//===============================================================================
// Copyright © Steven Robbins.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TinyMessenger {
    public sealed class TinyMessengerHub : ITinyMessengerHub {
        private readonly object _SubscriptionsPadlock = new object();
        private readonly List<SubscriptionItem> _Subscriptions = new List<SubscriptionItem>();
        private readonly Dictionary<object, List<TinyMessageSubscriptionToken>> _Listeners = new Dictionary<object, List<TinyMessageSubscriptionToken>>();
        private readonly IReportMessageDeliveryExceptions _ExceptionReporter;

        #region Public API

        public TinyMessengerHub() : this(null) {
        }

        public TinyMessengerHub(IReportMessageDeliveryExceptions exceptionReporter) {
            _ExceptionReporter = exceptionReporter;
        }

        public void Register(object listener) {
            if (listener == null) {
                throw new ArgumentNullException("listener");
            }

            Dictionary<Type, List<Action<object>>> methodsInSubscriber = FindAllSubscribeMethods(listener);
            List<TinyMessageSubscriptionToken> tokens = new List<TinyMessageSubscriptionToken>();

            foreach (var key in methodsInSubscriber.Keys) {
                List<Action<object>> actions = methodsInSubscriber[key];

                foreach (var action in actions) {
                    var subscribeInternalMethod = MakeGenericSubscribeInternalMethodWithType(key);
                    Func<object, bool> allowAllMessageFilter = (m) => true;
                    var subscribeInternalArguments = new object[] {
                        action, 
                        allowAllMessageFilter, 
                        true, 
                        DefaultTinyMessageProxy.Instance
                    };
                    TinyMessageSubscriptionToken token = (TinyMessageSubscriptionToken)subscribeInternalMethod.Invoke(this, subscribeInternalArguments);

                    tokens.Add(token);
                }
            }

            lock (_SubscriptionsPadlock) {
                if (_Listeners.ContainsKey(listener)) {
                    Unregister(listener);
                }
                _Listeners.Add(listener, tokens);
            }
        }

        public void Unregister(object listener) {
            if (listener == null) {
                throw new ArgumentNullException("listener");
            }

            lock (_SubscriptionsPadlock) {
                if (!_Listeners.ContainsKey(listener)) {
                    return;
                }

                List<TinyMessageSubscriptionToken> tokens = _Listeners[listener];

                foreach (var token in tokens) {
                    token.Dispose();
                }
                _Listeners.Remove(listener);
            }
        }

        public TinyMessageSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction) where TMessage : class {
            return AddSubscriptionInternal<TMessage>(deliveryAction, (m) => true, true, DefaultTinyMessageProxy.Instance);
        }

        public TinyMessageSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, ITinyMessageProxy proxy) where TMessage : class {
            return AddSubscriptionInternal<TMessage>(deliveryAction, (m) => true, true, proxy);
        }

        public TinyMessageSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, bool useStrongReferences) where TMessage : class {
            return AddSubscriptionInternal<TMessage>(deliveryAction, (m) => true, useStrongReferences, DefaultTinyMessageProxy.Instance);
        }

        public TinyMessageSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, bool useStrongReferences, ITinyMessageProxy proxy) where TMessage : class {
            return AddSubscriptionInternal<TMessage>(deliveryAction, (m) => true, useStrongReferences, proxy);
        }

        public TinyMessageSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter) where TMessage : class {
            return AddSubscriptionInternal<TMessage>(deliveryAction, messageFilter, true, DefaultTinyMessageProxy.Instance);
        }

        public TinyMessageSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, ITinyMessageProxy proxy) where TMessage : class {
            return AddSubscriptionInternal<TMessage>(deliveryAction, messageFilter, true, proxy);
        }

        public TinyMessageSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, bool useStrongReferences) where TMessage : class {
            return AddSubscriptionInternal<TMessage>(deliveryAction, messageFilter, useStrongReferences, DefaultTinyMessageProxy.Instance);
        }

        public TinyMessageSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, bool useStrongReferences, ITinyMessageProxy proxy) where TMessage : class {
            return AddSubscriptionInternal<TMessage>(deliveryAction, messageFilter, useStrongReferences, proxy);
        }

        public void Unsubscribe<TMessage>(TinyMessageSubscriptionToken subscriptionToken) where TMessage : class {
            RemoveSubscriptionInternal<TMessage>(subscriptionToken);
        }

        public void Publish<TMessage>(TMessage message) where TMessage : class {
            PublishInternal<TMessage>(message);
        }

        public void PublishAsync<TMessage>(TMessage message) where TMessage : class {
            PublishAsyncInternal<TMessage>(message, null);
        }

        public void PublishAsync<TMessage>(TMessage message, AsyncCallback callback) where TMessage : class {
            PublishAsyncInternal<TMessage>(message, callback);
        }

        #endregion

        #region Private Types and Interfaces

        private class WeakTinyMessageSubscription<TMessage> : ITinyMessageSubscription
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

        private class StrongTinyMessageSubscription<TMessage> : ITinyMessageSubscription
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

        private class SubscriptionItem {
            public ITinyMessageProxy Proxy { get; private set; }

            public ITinyMessageSubscription Subscription { get; private set; }

            public SubscriptionItem(ITinyMessageProxy proxy, ITinyMessageSubscription subscription) {
                Proxy = proxy;
                Subscription = subscription;
            }
        }

        #endregion

        #region Internal Methods

        private TinyMessageSubscriptionToken AddSubscriptionInternal<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, bool strongReference, ITinyMessageProxy proxy)
                where TMessage : class {
            if (deliveryAction == null)
                throw new ArgumentNullException("deliveryAction");

            if (messageFilter == null)
                throw new ArgumentNullException("messageFilter");

            if (proxy == null)
                throw new ArgumentNullException("proxy");

            lock (_SubscriptionsPadlock) {
                var subscriptionToken = new TinyMessageSubscriptionToken(this, typeof(TMessage));

                ITinyMessageSubscription subscription;
                if (strongReference)
                    subscription = new StrongTinyMessageSubscription<TMessage>(subscriptionToken, deliveryAction, messageFilter);
                else
                    subscription = new WeakTinyMessageSubscription<TMessage>(subscriptionToken, deliveryAction, messageFilter);

                _Subscriptions.Add(new SubscriptionItem(proxy, subscription));

                return subscriptionToken;
            }
        }

        private void RemoveSubscriptionInternal<TMessage>(TinyMessageSubscriptionToken subscriptionToken)
                where TMessage : class {
            if (subscriptionToken == null)
                throw new ArgumentNullException("subscriptionToken");

            lock (_SubscriptionsPadlock) {
                var currentlySubscribed = (from sub in _Subscriptions
                                                       where object.ReferenceEquals(sub.Subscription.SubscriptionToken, subscriptionToken)
                                                       select sub).ToList();

                foreach (SubscriptionItem subscriptionItem in currentlySubscribed.ToList()) {
                    _Subscriptions.Remove(subscriptionItem);
                }
            }
        }

        private void PublishInternal<TMessage>(TMessage message)
                where TMessage : class {
            if (message == null)
                throw new ArgumentNullException("message");

            List<SubscriptionItem> currentlySubscribed;
            lock (_SubscriptionsPadlock) {
                currentlySubscribed = (from sub in _Subscriptions
                                                   where sub.Subscription.ShouldAttemptDelivery(message)
                                                   select sub).ToList();
            }

            foreach (SubscriptionItem sub in currentlySubscribed) {
                try {
                    sub.Proxy.Deliver(message, sub.Subscription);
                } catch (Exception) {
                    // Ignore any errors and carry on
                    // TODO - add to a list of erroring subs and remove them?
                }
            }
        }

        private void PublishAsyncInternal<TMessage>(TMessage message, AsyncCallback callback) where TMessage : class {
            Action publishAction = () => {
                PublishInternal<TMessage>(message);
            };

            publishAction.BeginInvoke(callback, null);
        }

        private Dictionary<Type, List<Action<object>>> FindAllSubscribeMethods(object listener) {
            var result = new Dictionary<Type, List<Action<object>>>();

            foreach (MethodInfo method in GetMarkedMethods(listener)) {
                ParameterInfo[] parmetersTypes = method.GetParameters();
                Type eventType = parmetersTypes[0].ParameterType;
                Action<object> action = (e) => {
                    method.Invoke(listener, new object[] { e });
                };
                List<Action<object>> actions = null;

                if (result.ContainsKey(eventType)) {
                    actions = result[eventType];
                    actions.Add(action);
                } else {
                    actions = new List<Action<object>>();
                    actions.Add(action);
                    result.Add(eventType, actions);
                }
            }

            return result;
        }

        private MethodInfo MakeGenericSubscribeInternalMethodWithType(Type genericType) {
            IEnumerable<MethodInfo> subscribeInternalMethods = this.GetType().GetRuntimeMethods().Where<MethodInfo>(method => {
                return method.Name == "AddSubscriptionInternal" ? true : false;
            });
            return subscribeInternalMethods.First().MakeGenericMethod(genericType);
        }

        private IEnumerable<MethodInfo> GetMarkedMethods(object listener) {
            Type typeOfClass = listener.GetType();
            return typeOfClass.GetRuntimeMethods().Where<MethodInfo>((method) => {
                Attribute attribute = method.GetCustomAttribute(typeof(Subscribe));
                return attribute == null ? false : true;
            });
        }

        #endregion
    }
}
