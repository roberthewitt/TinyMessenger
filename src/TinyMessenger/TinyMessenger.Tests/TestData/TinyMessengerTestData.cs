using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyMessenger;

namespace TinyMessenger.Tests.TestData {
    public class TestMessage {
        public TestMessage() {
        }
    }

    public class DerivedMessage<TThings> : TestMessage {
        public TThings Things { get; set; }

        public DerivedMessage(object sender) {
        }
    }

    public interface ITestMessageInterface {
        
    }

    public class InterfaceDerivedMessage<TThings> : ITestMessageInterface {
        public object Sender { get; private set; }

        public TThings Things { get; set; }

        public InterfaceDerivedMessage(object sender) {
            this.Sender = sender;
        }
    }

    public class TestProxy : ITinyMessageProxy {
        public object Message { get; private set; }

        public void Deliver(object message, ITinyMessageSubscription subscription) {
            this.Message = message;
            subscription.Deliver(message);
        }
    }

    public class TestMessageListener {
        public TestMessage ReceivedTestMessage;

        [Subscribe]
        public void OnTestMessage(TestMessage message) {
            ReceivedTestMessage = message;
        }
    }

    public class TestMessageMultiListener {
        public TestMessage ReceivedMessageOne;
        public DerivedMessage<string> ReceivedMessageTwo;

        [Subscribe]
        public void OnMessageOne(TestMessage message) {
            ReceivedMessageOne = message;
        }

        [Subscribe]
        public void OnMessageTwo(DerivedMessage<string> message) {
            ReceivedMessageTwo = message;
        }
    }

    public class MessageDeliveryExceptionReporter : IReportMessageDeliveryExceptions {
        public Exception ReportedException;

        public void ReportException(Exception exception) {
            ReportedException = exception;
        }
    }

    public class ExceptionThrowingListener {
        [Subscribe]
        public void OnTestMessage(TestMessage message) {
            throw new Exception();
        }
    }

    public class ListenerWithASubscriptionMethodWithoutArguments {
        [Subscribe]
        public void OnNoMessage() {
        }
    }

    public class ListenerWithASubscriptionMethodWithAPrimitiveArgument {
        [Subscribe]
        public void OnAnInt(int number) {
        }
    }

    public class MainThreadListener {
        public TestMessage ReceivedTestMessage;

        [Subscribe, MainThread]
        public void OnTestMessageMain(TestMessage message) {
            ReceivedTestMessage = message;
        }
    }
}
