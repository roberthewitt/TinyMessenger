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
        public void Subscribe_ValidDeliverAction_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>));
        }

        [Test]
        public void SubScribe_ValidDeliveryAction_ReturnsRegistrationObject() {
            var messenger = UtilityMethods.GetMessenger();

            var output = messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>));

            Assert.That(output, Is.InstanceOf<TinyMessageSubscriptionToken>());
        }

        [Test]
        public void Subscribe_ValidDeliverActionWIthStrongReferences_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), true);
        }

        [Test]
        public void Subscribe_ValidDeliveryActionAndFilter_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Subscribe_NullDeliveryAction_Throws() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Subscribe<TestMessage>(null, new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Subscribe_NullFilter_Throws() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), null, new TestProxy());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Subscribe_NullProxy_Throws() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>), null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Unsubscribe_NullSubscriptionObject_Throws() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.Unsubscribe<TestMessage>(null);
        }

        [Test]
        public void Unsubscribe_PreviousSubscription_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var subscription = messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>));

            messenger.Unsubscribe<TestMessage>(subscription);
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
        public void Subscribe_PreviousSubscription_ReturnsDifferentSubscriptionObject() {
            var messenger = UtilityMethods.GetMessenger();
            var sub1 = messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>));
            var sub2 = messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>));

            Assert.IsFalse(object.ReferenceEquals(sub1, sub2));
        }

        [Test]
        public void Subscribe_CustomProxyNoFilter_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var proxy = new TestProxy();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), proxy);
        }

        [Test]
        public void Subscribe_CustomProxyWithFilter_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var proxy = new TestProxy();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>), proxy);
        }

        [Test]
        public void Subscribe_CustomProxyNoFilterStrongReference_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var proxy = new TestProxy();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), true, proxy);
        }

        [Test]
        public void Subscribe_CustomProxyFilterStrongReference_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var proxy = new TestProxy();

            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>), true, proxy);
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
        public void Publish_CustomProxyNoFilter_UsesCorrectProxy() {
            var messenger = UtilityMethods.GetMessenger();
            var proxy = new TestProxy();
            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), proxy);
            var message = new TestMessage();

            messenger.Publish<TestMessage>(message);

            Assert.AreSame(message, proxy.Message);
        }

        [Test]
        public void Publish_CustomProxyWithFilter_UsesCorrectProxy() {
            var messenger = UtilityMethods.GetMessenger();
            var proxy = new TestProxy();
            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>), proxy);
            var message = new TestMessage();

            messenger.Publish<TestMessage>(message);

            Assert.AreSame(message, proxy.Message);
        }

        [Test]
        public void Publish_CustomProxyNoFilterStrongReference_UsesCorrectProxy() {
            var messenger = UtilityMethods.GetMessenger();
            var proxy = new TestProxy();
            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), true, proxy);
            var message = new TestMessage();

            messenger.Publish<TestMessage>(message);

            Assert.AreSame(message, proxy.Message);
        }

        [Test]
        public void Publish_CustomProxyFilterStrongReference_UsesCorrectProxy() {
            var messenger = UtilityMethods.GetMessenger();
            var proxy = new TestProxy();
            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>), true, proxy);
            var message = new TestMessage();

            messenger.Publish<TestMessage>(message);

            Assert.AreSame(message, proxy.Message);
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
        public void Publish_Subscriber_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            messenger.Subscribe<TestMessage>(new Action<TestMessage>(UtilityMethods.FakeDeliveryAction<TestMessage>), new Func<TestMessage, bool>(UtilityMethods.FakeMessageFilter<TestMessage>));

            messenger.Publish<TestMessage>(new TestMessage());
        }

        [Test]
        public void Publish_SubscribedMessageNoFilter_GetsMessage() {
            var messenger = UtilityMethods.GetMessenger();
            bool received = false;
            messenger.Subscribe<TestMessage>((m) => {
                    received = true;
                });

            messenger.Publish<TestMessage>(new TestMessage());

            Assert.IsTrue(received);
        }

        [Test]
        public void Publish_SubscribedThenUnsubscribedMessageNoFilter_DoesNotGetMessage() {
            var messenger = UtilityMethods.GetMessenger();
            bool received = false;
            var token = messenger.Subscribe<TestMessage>((m) => {
                    received = true;
                });
            messenger.Unsubscribe<TestMessage>(token);

            messenger.Publish<TestMessage>(new TestMessage());

            Assert.IsFalse(received);
        }

        [Test]
        public void Publish_SubscribedMessageButFiltered_DoesNotGetMessage() {
            var messenger = UtilityMethods.GetMessenger();
            bool received = false;
            messenger.Subscribe<TestMessage>((m) => {
                    received = true;
                }, (m) => false);

            messenger.Publish<TestMessage>(new TestMessage());

            Assert.IsFalse(received);
        }

        [Test]
        public void Publish_SubscribedMessageNoFilter_GetsActualMessage() {
            var messenger = UtilityMethods.GetMessenger();
            object receivedMessage = null;
            var payload = new TestMessage();
            messenger.Subscribe<TestMessage>((m) => {
                    receivedMessage = m;
                });

            messenger.Publish<TestMessage>(payload);

            Assert.AreSame(payload, receivedMessage);
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
        public void GenericTinyMessage_String_SubscribeDoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            var output = string.Empty;
            messenger.Subscribe<GenericTinyMessage<string>>((m) => {
                    output = m.Content;
                });
        }

        [Test]
        public void GenericTinyMessage_String_PubishDoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            messenger.Publish(new GenericTinyMessage<string>("Testing"));
        }

        [Test]
        public void GenericTinyMessage_String_PubishAndSubscribeDeliversContent() {
            var messenger = UtilityMethods.GetMessenger();
            var output = string.Empty;
            messenger.Subscribe<GenericTinyMessage<string>>((m) => {
                    output = m.Content;
                });
            messenger.Publish(new GenericTinyMessage<string>("Testing"));

            Assert.AreEqual("Testing", output);
        }

        [Test]
        public void Publish_SubscriptionThrowingException_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
            messenger.Subscribe<GenericTinyMessage<string>>((m) => {
                    throw new NotImplementedException();
                });

            messenger.Publish(new GenericTinyMessage<string>("Testing"));
        }

        [Test]
        public void PublishAsync_NoCallback_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();

            messenger.PublishAsync(new TestMessage());
        }

        [Test]
        public void PublishAsync_NoCallback_PublishesMessage() {
            var messenger = UtilityMethods.GetMessenger();
            bool received = false;
            messenger.Subscribe<TestMessage>((m) => {
                    received = true;
                });

            messenger.PublishAsync(new TestMessage());

            // Horrible wait loop!
            int waitCount = 0;
            while (!received && waitCount < 100) {
                Thread.Sleep(10);
                waitCount++;
            }
            Assert.IsTrue(received);
        }

        [Test]
        public void PublishAsync_Callback_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
#pragma warning disable 219
            messenger.PublishAsync(new TestMessage(), (r) => {
                    string test = "Testing";
                });
#pragma warning restore 219
        }

        [Test]
        public void PublishAsync_Callback_PublishesMessage() {
            var messenger = UtilityMethods.GetMessenger();
            bool received = false;
            messenger.Subscribe<TestMessage>((m) => {
                    received = true;
                });

#pragma warning disable 219
            messenger.PublishAsync(new TestMessage(), (r) => {
                    string test = "Testing";
                });
#pragma warning restore 219

            // Horrible wait loop!
            int waitCount = 0;
            while (!received && waitCount < 100) {
                Thread.Sleep(10);
                waitCount++;
            }
            Assert.IsTrue(received);
        }

        [Test]
        public void PublishAsync_Callback_CallsCallback() {
            var messenger = UtilityMethods.GetMessenger();
            bool received = false;
            bool callbackReceived = false;
            messenger.Subscribe<TestMessage>((m) => {
                    received = true;
                });

            messenger.PublishAsync(new TestMessage(), (r) => {
                    callbackReceived = true;
                });

            // Horrible wait loop!
            int waitCount = 0;
            while (!callbackReceived && waitCount < 100) {
                Thread.Sleep(10);
                waitCount++;
            }
            Assert.IsTrue(received);
        }

        [Test]
        public void CancellableGenericTinyMessage_Publish_DoesNotThrow() {
            var messenger = UtilityMethods.GetMessenger();
#pragma warning disable 219
            messenger.Publish<CancellableGenericTinyMessage<string>>(new CancellableGenericTinyMessage<string>("Testing", () => {
                        bool test = true;
                    }));
#pragma warning restore 219
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CancellableGenericTinyMessage_PublishWithNullAction_Throws() {
            var messenger = UtilityMethods.GetMessenger();
            messenger.Publish<CancellableGenericTinyMessage<string>>(new CancellableGenericTinyMessage<string>("Testing", null));
        }

        [Test]
        public void CancellableGenericTinyMessage_SubscriberCancels_CancelActioned() {
            var messenger = UtilityMethods.GetMessenger();
            bool cancelled = false;
            messenger.Subscribe<CancellableGenericTinyMessage<string>>((m) => {
                    m.Cancel();
                });

            messenger.Publish<CancellableGenericTinyMessage<string>>(new CancellableGenericTinyMessage<string>("Testing", () => {
                        cancelled = true;
                    }));

            Assert.IsTrue(cancelled);
        }

        [Test]
        public void CancellableGenericTinyMessage_SeveralSubscribersOneCancels_CancelActioned() {
            var messenger = UtilityMethods.GetMessenger();
            bool cancelled = false;
#pragma warning disable 219
            messenger.Subscribe<CancellableGenericTinyMessage<string>>((m) => {
                    var test = 1;
                });
            messenger.Subscribe<CancellableGenericTinyMessage<string>>((m) => {
                    m.Cancel();
                });
            messenger.Subscribe<CancellableGenericTinyMessage<string>>((m) => {
                    var test = 1;
                });
#pragma warning restore 219
            messenger.Publish<CancellableGenericTinyMessage<string>>(new CancellableGenericTinyMessage<string>("Testing", () => {
                        cancelled = true;
                    }));

            Assert.IsTrue(cancelled);
        }

        [Test]
        public void Publish_SubscriptionOnBaseClass_HitsSubscription() {
            var received = false;
            var messenger = UtilityMethods.GetMessenger();
            messenger.Subscribe<TestMessage>(tm => received = true);

            messenger.Publish(new DerivedMessage<string>(this) { Things = "Hello" });

            Assert.IsTrue(received);
        }

        [Test]
        public void Publish_SubscriptionOnImplementedInterface_HitsSubscription() {
            var received = false;
            var messenger = UtilityMethods.GetMessenger();
            messenger.Subscribe<ITestMessageInterface>(tm => received = true);

            messenger.Publish(new InterfaceDerivedMessage<string>(this) { Things = "Hello" });

            Assert.IsTrue(received);
        }

        [Test]
        public void Publish_SubscribedOnMainThread_MainThreadMessageProxyDoesDelivery() {
            var messenger = UtilityMethods.GetMessenger();
            var mockMainThreadMessageProxy = new Mock<ITinyMessageProxy>();
            var message = new TestMessage();
            messenger.MainThreadTinyMessageProxy = mockMainThreadMessageProxy.Object;

            messenger.SubscribeOnMainThread<TestMessage>(tm => {});
            messenger.Publish(message);

            mockMainThreadMessageProxy.Verify(mockProxy => mockProxy.Deliver(message, It.IsAny<ITinyMessageSubscription>()), Times.Exactly(1));
        }
    }
}
