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

        public ITinyMessageProxy MainThreadTinyMessageProxy { get; set; }

        public IHandleThreading ThreadHandler { get; set; }

        public TinyMessengerHub() : this(null) {
        }

        public TinyMessengerHub(IReportMessageDeliveryExceptions exceptionReporter) {
            _ExceptionReporter = exceptionReporter;
        }

        public void Register(object listener) {
            if (listener == null) {
                throw new ArgumentNullException("listener");
            }

            Dictionary<Type, List<SubscriberAction>> methodsInSubscriber = SubscriberActionExtractor.FindAll(listener, MainThreadTinyMessageProxy);
            List<TinyMessageSubscriptionToken> tokens = new List<TinyMessageSubscriptionToken>();

            foreach (var key in methodsInSubscriber.Keys) {
                List<SubscriberAction> subscriberActions = methodsInSubscriber[key];

                foreach (var subscriberAction in subscriberActions) {
                    var subscribeInternalMethod = MakeGenericSubscribeInternalMethodWithType(key);
                    Func<object, bool> allowAllMessageFilter = (m) => true;
                    var subscribeInternalArguments = new object[] {
                        subscriberAction.Action, 
                        allowAllMessageFilter, 
                        true, 
                        subscriberAction.Proxy
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

        public void Unsubscribe<TMessage>(TinyMessageSubscriptionToken subscriptionToken) where TMessage : class {
            RemoveSubscriptionInternal<TMessage>(subscriptionToken);
        }

        public void Publish<TMessage>(TMessage message) where TMessage : class {
            PublishInternal<TMessage>(message);
        }

        public void PublishAsync<TMessage>(TMessage message) where TMessage : class {
            PublishAsyncInternal<TMessage>(message, null);
        }

        private void PublishAsync<TMessage>(TMessage message, AsyncCallback callback) where TMessage : class {
            PublishAsyncInternal<TMessage>(message, callback);
        }

        #endregion

        #region Private Types and Interfaces

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
                } catch (Exception exception) {
                    if (_ExceptionReporter != null) {
                        _ExceptionReporter.ReportException(exception);
                    }
                }
            }
        }

        private void PublishAsyncInternal<TMessage>(TMessage message, AsyncCallback callback) where TMessage : class {
            Action publishAction = () => {
                PublishInternal<TMessage>(message);
            };

            publishAction.BeginInvoke(callback, null);
        }

        private MethodInfo MakeGenericSubscribeInternalMethodWithType(Type genericType) {
            IEnumerable<MethodInfo> subscribeInternalMethods = this.GetType().GetRuntimeMethods().Where<MethodInfo>(method => {
                    return method.Name == "AddSubscriptionInternal" ? true : false;
                });
            return subscribeInternalMethods.First().MakeGenericMethod(genericType);
        }

        #endregion
    }
}
