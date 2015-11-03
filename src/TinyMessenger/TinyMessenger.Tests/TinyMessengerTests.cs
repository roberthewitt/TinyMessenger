using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TinyMessenger.Tests.TestData;
using TinyMessenger;
using System.Threading;
using Moq;

namespace TinyMessenger.Tests {

    [TestFixture]
    public class TinyMessengerTests {
        [Test]
        public void TinyMessenger_Ctor_DoesNotThrow() {
            UtilityMethods.GetMessenger();
        }

        [Test]
        public void Ctor_WithMessageDeliveryExceptionReporter_DoesNotThrow() {
            var exceptionReporter = new MessageDeliveryExceptionReporter();
            var hub = new TinyMessengerHub(exceptionReporter);

            Assert.IsNotNull(hub);
        }

        [Test]
        public void Unregister_ListenerWithAnnotatedMethod_ListenerDoesNotReceivePostedMessage() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = UtilityMethods.GetListener();
            var payload = new TestMessage();

            messenger.Register(listener);
            messenger.Unregister(listener);
            messenger.Publish<TestMessage>(payload);

            Assert.IsNull(listener.ReceivedTestMessage);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Unregister_NulListener_Throws() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Unregister(null);
        }

        [Test]
        public void Unregister_NotAListener_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var notAListener = new Object();

            messenger.Unregister(notAListener);
        }

        [Test]
        public void Register_ListenerWithAnnotatedMethod_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = UtilityMethods.GetListener();

            messenger.Register(listener);
        }

        [Test]
        public void Register_ListenerWithMultipleAnnotatedMethods_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = new Object();

            messenger.Register(listener);
        }

        [Test]
        public void Register_ListenerWithNoAnnotatedMethods_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = new Object();

            messenger.Register(listener);
        }

        [Test]
        public void Register_ListenerTwice_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = UtilityMethods.GetListener();

            messenger.Register(listener);
            messenger.Register(listener);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Register_NullListener_Throws() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Register(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Register_ListenerWithASubscriptionMethodWithoutArguments_Throws() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = new ListenerWithASubscriptionMethodWithoutArguments();

            messenger.Register(listener);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Register_ListenerWithASubscriptionMethodWithAPrimitiveArgument_Throws() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = new ListenerWithASubscriptionMethodWithAPrimitiveArgument();

            messenger.Register(listener);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Publish_NullMessage_Throws() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Publish<TestMessage>(null);
        }

        [Test]
        public void Publish_NoSubscribers_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Publish<TestMessage>(new TestMessage());
        }

        [Test]
        public void Publish_RegisteredListenerWithAnnotatedMethod_SubscribedMethodGetsActualMessage() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = UtilityMethods.GetListener();
            var payload = new TestMessage();
            messenger.Register(listener);

            messenger.Publish<TestMessage>(payload);

            Assert.AreSame(payload, listener.ReceivedTestMessage);
        }

        [Test]
        public void Publish_ExceptionThrownByMessageHandler_DoesNotThrow() {
            var hub = UtilityMethods.GetMessenger();
            var listener = new ExceptionThrowingListener();

            hub.Register(listener);
            hub.Publish(new TestMessage());
        }

        [Test]
        public void Publish_ExceptionThrownByMessageHandler_PassedToExceptionReporter() {
            var exceptionReporter = new MessageDeliveryExceptionReporter();
            var hub = new TinyMessengerHub(exceptionReporter);
            var listener = new ExceptionThrowingListener();

            hub.Register(listener);
            hub.Publish(new TestMessage());

            Assert.IsNotNull(exceptionReporter.ReportedException);
        }

        [Test]
        public void PublishAsync_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.PublishAsync(new TestMessage());
        }


        private class RemembersCallStateHandler {

            public Boolean HasBeenCalled {get;set;} = false;

            [Subscribe]
            public void OnTestMessage(TestMessage message) {
                HasBeenCalled = true;
            }
        }

        [Test]
        public void PublishAsync_PublishesMessage() {
            var messenger = UtilityMethods.GetMessenger();
            var a = new RemembersCallStateHandler();
            messenger.Register(a);

            messenger.PublishAsync(new TestMessage());

            // Horrible wait loop!
            int waitCount = 0;
            while (!a.HasBeenCalled && waitCount < 100) {
                Thread.Sleep(10);
                waitCount++;
            }
            Assert.IsTrue(a.HasBeenCalled);
        }

        [Test]
        public void CancellableGenericTinyMessage_Publish_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Publish<CancellableGenericTinyMessage<string>>(new CancellableGenericTinyMessage<string>("Testing", () => {}));
        }

        [Test]
        public void Publish_RegisteredWithSubscribeOnMainThread_MainThreadMessageProxyDoesDelivery() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = new MainThreadListener();
            var mainThread = new Mock<ITinyMessageProxy>();
            Mock<IHandleThreading> threadHandler = new Mock<IHandleThreading>();
            threadHandler
                .Setup(t => t.MainThread())
                .Returns(mainThread.Object);
            
            messenger.ThreadHandler = threadHandler.Object;

            messenger.Register(listener);

            var message = new TestMessage();
            messenger.Publish(message);

            mainThread.Verify(p => p.Deliver(message, It.IsAny<ITinyMessageSubscription>()), Times.Exactly(1));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Register_WithSubscribeOnMainThreadButMainThreadMessageProxyIsNull_Throws() {
            var messenger = UtilityMethods.GetMessenger();
            var listener = new MainThreadListener();

            messenger.Register(listener);
        }
    }
}
