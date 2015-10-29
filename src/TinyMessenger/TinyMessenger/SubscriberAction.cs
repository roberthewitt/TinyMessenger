using System;

namespace TinyMessenger {
    internal class SubscriberAction {
        public Action<object> Action = null;
        public ITinyMessageProxy Proxy = DefaultTinyMessageProxy.Instance;
    }
}
