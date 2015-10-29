using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace TinyMessenger {
    internal static class SubscriberActionExtractor {
        public static Dictionary<Type, List<SubscriberAction>> FindAll(object listener) {
            var result = new Dictionary<Type, List<SubscriberAction>>();

            foreach (MethodInfo method in GetMarkedMethods(listener)) {
                ParameterInfo[] parmetersTypes = method.GetParameters();
                Type eventType = parmetersTypes[0].ParameterType;
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
