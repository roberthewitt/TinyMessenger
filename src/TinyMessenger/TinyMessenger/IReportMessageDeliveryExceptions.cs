using System;

namespace TinyMessenger {
    public interface IReportMessageDeliveryExceptions {
        void ReportException(Exception exception);
    }
}
