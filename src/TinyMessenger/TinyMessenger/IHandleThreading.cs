using System;

namespace TinyMessenger {
    public interface IHandleThreading {
        ITinyMessageProxy MainThread();

        ITinyMessageProxy BackgroundThread();
    }
}