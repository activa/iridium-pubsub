using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Iridium.PubSub.Test
{
    [TestFixture]
    public class MessageBrokerTest
    {
        private class TestSubscriber
        {
            private readonly StringBuilder _sb;

            public TestSubscriber()
            {
                _sb = new StringBuilder();
            }

            public TestSubscriber(StringBuilder sb)
            {
                _sb = sb;
            }

            public int Calls;
            public string Result => _sb.ToString();

            public void Reset()
            {
                _sb.Clear();
                Calls = 0;
            }

            public void Append(string s)
            {
                _sb.Append(s);
                Calls++;
            }
        }

        void Check(MsgBroker hub, TestSubscriber subscriber, Action<MsgBroker> action, int expectedCalls, string expectedResult = null)
        {
            subscriber.Reset();

            action(hub);
            
            Assert.That(subscriber.Calls, Is.EqualTo(expectedCalls));

            if (expectedResult != null)
                Assert.That(subscriber.Result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void NamedNoParam()
        {
            var subscriber = new TestSubscriber();

            MsgBroker createHub()
            {
                var h = new MsgBroker();

                h.Subscribe(subscriber, "x", () => subscriber.Append("A"));
                h.Subscribe(subscriber, "y", () => subscriber.Append("B"));

                return h;
            }

            var hub = createHub();

            Check(hub, subscriber, h => h.Publish("x"), 1, "A");
            Check(hub, subscriber, h => h.Publish("y"), 1, "B");
            Check(hub, subscriber, h => h.Publish("z"), 0);
            Check(hub, subscriber, h => h.PublishObject(new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("x", new DateTime()), 1, "A");

            hub = createHub();

            hub.Unsubscribe(subscriber, "x");

            Check(hub, subscriber, h => h.Publish("x"), 0);
            Check(hub, subscriber, h => h.Publish("y"), 1, "B");
            Check(hub, subscriber, h => h.Publish("z"), 0);
            Check(hub, subscriber, h => h.PublishObject(new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("x", new DateTime()), 0);

            hub = createHub();

            hub.Unsubscribe(subscriber);

            Check(hub, subscriber, h => h.Publish("x"), 0);
            Check(hub, subscriber, h => h.Publish("y"), 0);
            Check(hub, subscriber, h => h.Publish("z"), 0);
            Check(hub, subscriber, h => h.PublishObject(new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("x", new DateTime()), 0);

        }

        [Test]
        public void RegexNoParam()
        {
            var subscriber = new TestSubscriber();

            var hub = new MsgBroker();

            hub.Subscribe(subscriber, "x", () => subscriber.Append("A"));
            hub.Subscribe(subscriber, "y", () => subscriber.Append("B"));
            hub.Subscribe(subscriber, new Regex("x|y"), () => subscriber.Append("C"));

            Check(hub, subscriber, h => h.Publish("x"), 2, "AC");
            Check(hub, subscriber, h => h.Publish("y"), 2, "BC");
            Check(hub, subscriber, h => h.Publish("z"), 0);
            Check(hub, subscriber, h => h.PublishObject(new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("x", new DateTime()), 2, "AC");

            hub.Unsubscribe(subscriber, new Regex("x|y"));

            Check(hub, subscriber, h => h.Publish("x"), 1, "A");
            Check(hub, subscriber, h => h.Publish("y"), 1, "B");
            Check(hub, subscriber, h => h.Publish("z"), 0);
            Check(hub, subscriber, h => h.PublishObject(new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("x", new DateTime()), 1, "A");
        }

        [Test]
        public void TypedUnnamed()
        {
            var subscriber = new TestSubscriber();

            MsgBroker createHub()
            {
                var h = new MsgBroker();

                h.Subscribe<string>(subscriber, s => subscriber.Append($"A({s})"));
                h.Subscribe<int>(subscriber, i => subscriber.Append($"B({i})"));

                return h;
            }

            var hub = createHub();

            Check(hub, subscriber, h => h.PublishObject("x"), 1, "A(x)");
            Check(hub, subscriber, h => h.PublishObject("y"), 1, "A(y)");
            Check(hub, subscriber, h => h.PublishObject(123), 1, "B(123)");
            Check(hub, subscriber, h => h.Publish("X"), 0);
            Check(hub, subscriber, h => h.Publish("Y"), 0);
            Check(hub, subscriber, h => h.PublishObject("Z", "z"), 1, "A(z)");
            Check(hub, subscriber, h => h.PublishObject("Z", 123), 1, "B(123)");
            Check(hub, subscriber, h => h.PublishObject(new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("x", new DateTime()), 0);

            hub = createHub();

            hub.Unsubscribe<string>(subscriber);

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject(123), 1, "B(123)");
            Check(hub, subscriber, h => h.Publish("X"), 0);
            Check(hub, subscriber, h => h.Publish("Y"), 0);
            Check(hub, subscriber, h => h.PublishObject("Z", "z"), 0);
            Check(hub, subscriber, h => h.PublishObject("Z", 123), 1, "B(123)");
            Check(hub, subscriber, h => h.PublishObject(new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("x", new DateTime()), 0);

            hub = createHub();

            hub.Unsubscribe(subscriber);

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject(123), 0);
            Check(hub, subscriber, h => h.Publish("X"), 0);
            Check(hub, subscriber, h => h.Publish("Y"), 0);
            Check(hub, subscriber, h => h.PublishObject("Z", "z"), 0);
            Check(hub, subscriber, h => h.PublishObject("Z", 123), 0);
            Check(hub, subscriber, h => h.PublishObject(new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("x", new DateTime()), 0);
        }

        [Test]
        public void TypedNamed()
        {
            var subscriber = new TestSubscriber();

            MsgBroker createHub()
            {
                var h = new MsgBroker();

                h.Subscribe<string>(subscriber, "A", s => subscriber.Append($"A({s})"));
                h.Subscribe<int>(subscriber, "B", i => subscriber.Append($"B({i})"));

                return h;
            }

            var hub = createHub();

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", "x"), 1, "A(x)");
            Check(hub, subscriber, h => h.PublishObject("B", "x"), 0);
            Check(hub, subscriber, h => h.PublishObject(123), 0);
            Check(hub, subscriber, h => h.PublishObject("A", 123), 0);
            Check(hub, subscriber, h => h.PublishObject("B", 123), 1, "B(123)");
            Check(hub, subscriber, h => h.Publish("A"), 0);
            Check(hub, subscriber, h => h.Publish("B"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("B", new DateTime()), 0);

            hub = createHub();

            hub.Unsubscribe<string>(subscriber);

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", "x"), 0);
            Check(hub, subscriber, h => h.PublishObject("B", "x"), 0);
            Check(hub, subscriber, h => h.PublishObject(123), 0);
            Check(hub, subscriber, h => h.PublishObject("A", 123), 0);
            Check(hub, subscriber, h => h.PublishObject("B", 123), 1, "B(123)");
            Check(hub, subscriber, h => h.Publish("A"), 0);
            Check(hub, subscriber, h => h.Publish("B"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("B", new DateTime()), 0);

            hub = createHub();

            hub.Unsubscribe<int>(subscriber);

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", "x"), 1, "A(x)");
            Check(hub, subscriber, h => h.PublishObject("B", "x"), 0);
            Check(hub, subscriber, h => h.PublishObject(123), 0);
            Check(hub, subscriber, h => h.PublishObject("A", 123), 0);
            Check(hub, subscriber, h => h.PublishObject("B", 123), 0);
            Check(hub, subscriber, h => h.Publish("A"), 0);
            Check(hub, subscriber, h => h.Publish("B"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("B", new DateTime()), 0);

            hub = createHub();

            hub.Unsubscribe(subscriber, "A");

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", "x"), 0);
            Check(hub, subscriber, h => h.PublishObject("B", "x"), 0);
            Check(hub, subscriber, h => h.PublishObject(123), 0);
            Check(hub, subscriber, h => h.PublishObject("A", 123), 0);
            Check(hub, subscriber, h => h.PublishObject("B", 123), 1, "B(123)");
            Check(hub, subscriber, h => h.Publish("A"), 0);
            Check(hub, subscriber, h => h.Publish("B"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("B", new DateTime()), 0);

            hub = createHub();

            hub.Unsubscribe(subscriber);

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", "x"), 0);
            Check(hub, subscriber, h => h.PublishObject("B", "x"), 0);
            Check(hub, subscriber, h => h.PublishObject(123), 0);
            Check(hub, subscriber, h => h.PublishObject("A", 123), 0);
            Check(hub, subscriber, h => h.PublishObject("B", 123), 0);
            Check(hub, subscriber, h => h.Publish("A"), 0);
            Check(hub, subscriber, h => h.Publish("B"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("B", new DateTime()), 0);
        }


        [Test]
        public void TypedRegex()
        {
            var subscriber = new TestSubscriber();

            MsgBroker createHub()
            {
                var h = new MsgBroker();

                h.Subscribe<string>(subscriber, "A", s => subscriber.Append($"A({s})"));
                h.Subscribe<int>(subscriber, "B", i => subscriber.Append($"B({i})"));
                h.Subscribe<string>(subscriber, "C", s => subscriber.Append($"B({s})"));
                h.Subscribe<int>(subscriber, new Regex("A|B"), i => subscriber.Append($"AB({i})"));
                h.Subscribe<string>(subscriber, new Regex("A|B"), s => subscriber.Append($"AB({s})"));
                h.Subscribe<int>(subscriber, new Regex("B|C"), i => subscriber.Append($"BC({i})"));
                h.Subscribe<string>(subscriber, new Regex("B|C"), s => subscriber.Append($"BC({s})"));

                return h;
            }

            var hub = createHub();

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", "x"), 2, "A(x)AB(x)");
            Check(hub, subscriber, h => h.PublishObject("B", "x"), 2, "AB(x)BC(x)");
            Check(hub, subscriber, h => h.PublishObject(123), 0);
            Check(hub, subscriber, h => h.PublishObject("A", 123), 1, "AB(123)");
            Check(hub, subscriber, h => h.PublishObject("B", 123), 3, "B(123)AB(123)BC(123)");
            Check(hub, subscriber, h => h.Publish("A"), 0);
            Check(hub, subscriber, h => h.Publish("B"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("B", new DateTime()), 0);

            hub = createHub();

            hub.Unsubscribe<string>(subscriber, new Regex("A|B"));

            Check(hub, subscriber, h => h.PublishObject("x"), 0);
            Check(hub, subscriber, h => h.PublishObject("y"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", "x"), 1, "A(x)");
            Check(hub, subscriber, h => h.PublishObject("B", "x"), 1, "BC(x)");
            Check(hub, subscriber, h => h.PublishObject(123), 0);
            Check(hub, subscriber, h => h.PublishObject("A", 123), 1, "AB(123)");
            Check(hub, subscriber, h => h.PublishObject("B", 123), 3, "B(123)AB(123)BC(123)");
            Check(hub, subscriber, h => h.Publish("A"), 0);
            Check(hub, subscriber, h => h.Publish("B"), 0);
            Check(hub, subscriber, h => h.PublishObject("A", new DateTime()), 0);
            Check(hub, subscriber, h => h.PublishObject("B", new DateTime()), 0);
        }

        [Test]
        public void MultipleTypes()
        {
            var subscriber = new TestSubscriber();

            var hub = new MsgBroker();

            hub.Subscribe(subscriber, "A", (int i) => subscriber.Append($"Ai({i})"));
            hub.Subscribe(subscriber, "A", (string s) => subscriber.Append($"As({s})"));
            hub.Subscribe(subscriber, "A", (object o) => subscriber.Append($"Ao({o})"));
            hub.Subscribe(subscriber, "A", () => subscriber.Append($"A()"));

            Check(hub, subscriber, h => h.PublishObject("A", 123), 3, "Ai(123)Ao(123)A()");
            Check(hub, subscriber, h => h.PublishObject("A", "x"), 3, "As(x)Ao(x)A()");
            Check(hub, subscriber, h => h.PublishObject("A", 123d), 2, "Ao(123)A()");
            Check(hub, subscriber, h => h.Publish("A"), 1, "A()");
        }


        [Test]
        public void TestDeadSubscriber()
        {
            StringBuilder sb = new StringBuilder();

            var hub = new MsgBroker();

            var subscriberA = new TestSubscriber(sb);
            var subscriberB = new TestSubscriber(sb);

            hub.Subscribe<string>(subscriberA, s => sb.Append($"A({s})"));
            hub.Subscribe<string>(subscriberB, s => sb.Append($"B({s})"));

            hub.PublishObject("x");
            hub.PublishObject("y");

            Assert.That(sb.ToString(), Is.EqualTo("A(x)B(x)A(y)B(y)"));

            subscriberA = null;

            GC.Collect();

            sb.Clear();

            hub.PublishObject("x");
            hub.PublishObject("y");

            Assert.That(sb.ToString(), Is.EqualTo("B(x)B(y)"));

            subscriberB.Reset(); // make sure subscriberB stays alive
        }
    }
}