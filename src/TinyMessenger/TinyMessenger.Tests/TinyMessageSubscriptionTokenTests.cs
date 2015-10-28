using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TinyMessenger.Tests.TestData;
using TinyMessenger;
using System.Threading;

namespace TinyMessenger.Tests
{
    [TestFixtureAttribute]
    public class TinyMessageSubscriptionTokenTests
    {
        [TestAttribute]
        public void Dispose_WithValidHubReference_UnregistersWithHub()
        {
            var messengerMock = new Moq.Mock<ITinyMessengerHub>();
            messengerMock.Setup((messenger) => messenger.Unsubscribe<TestMessage>(Moq.It.IsAny<TinyMessageSubscriptionToken>())).Verifiable();
            var token = new TinyMessageSubscriptionToken(messengerMock.Object, typeof(TestMessage));

            token.Dispose();

            messengerMock.VerifyAll();
        }

		[TestAttribute]
        public void Dispose_WithInvalidHubReference_DoesNotThrow()
        {
            var token = UtilityMethods.GetTokenWithOutOfScopeMessenger();
            GC.Collect();
			Thread.Sleep(2000);

            token.Dispose();
        }

		[TestAttribute]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullHub_ThrowsArgumentNullException()
        {
            UtilityMethods.GetMessenger();

			new TinyMessageSubscriptionToken(null, typeof(object));
        }

		[TestAttribute]
        public void Ctor_ValidHubAndMessageType_DoesNotThrow()
        {
            var messenger = UtilityMethods.GetMessenger();

            new TinyMessageSubscriptionToken(messenger, typeof(TestMessage));
        }
    }
}
