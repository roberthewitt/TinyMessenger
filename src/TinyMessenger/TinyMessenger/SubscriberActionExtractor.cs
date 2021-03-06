﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace TinyMessenger {
    internal static class SubscriberActionExtractor {
        public static Dictionary<Type, List<SubscriberAction>> FindAll(object listener, IHandleThreading threading) {
            var result = new Dictionary<Type, List<SubscriberAction>>();

            foreach (MethodInfo method in GetMarkedMethods(listener)) {
                ParameterInfo[] parmetersTypes = method.GetParameters();

                if (parmetersTypes.Length == 0) {
                    throw new ArgumentException("listener has a Subscribe method without arguments");
                }
                Type eventType = parmetersTypes[0].ParameterType;
                if (!eventType.GetTypeInfo().IsClass) {
                    throw new ArgumentException("listener has a Subscribe method with a non-class argument");
                }
                Action<object> action = (e) => {
                    method.Invoke(listener, new object[] { e });
                };
                List<SubscriberAction> subscriberActions = null;
                SubscriberAction subscriberAction = new SubscriberAction() { Action = action };

                if (result.ContainsKey(eventType)) {
                    subscriberActions = result[eventType];
                    subscriberActions.Add(subscriberAction);
                } else {
                    subscriberActions = new List<SubscriberAction>();
                    subscriberActions.Add(subscriberAction);
                    result.Add(eventType, subscriberActions);
                }

                if (method.GetCustomAttribute(typeof(MainThread)) != null) {
                    if (threading == null) {
                        throw new InvalidOperationException("Set ThreadHandler before Registering a class that Subscribes for a main Thread");
                    }
                    subscriberAction.Proxy = threading.MainThread();
                }
                if (method.GetCustomAttribute(typeof(BackgroundThread)) != null) {
                    if (threading == null) {
                        throw new InvalidOperationException("Set ThreadHandler before Registering a class that Subscribes for a background Thread");
                    }
                    subscriberAction.Proxy = threading.BackgroundThread();
                }
            }

            return result;
        }

        private static IEnumerable<MethodInfo> GetMarkedMethods(object listener) {
            Type typeOfClass = listener.GetType();
            return typeOfClass.GetRuntimeMethods().Where<MethodInfo>((method) => {
                Attribute attribute = method.GetCustomAttribute(typeof(Subscribe));
                return attribute == null ? false : true;
            });
        }
    }
}
