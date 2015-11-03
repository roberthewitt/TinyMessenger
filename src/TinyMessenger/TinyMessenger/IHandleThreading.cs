using System;

namespace TinyMessenger {
    public interface IHandleThreading {
        ITinyMessageProxy mainThread();

        ITinyMessageProxy backgroundThread();
    }
}