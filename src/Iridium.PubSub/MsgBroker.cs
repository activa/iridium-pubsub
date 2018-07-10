using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Iridium.PubSub
{
    public class MsgBroker
    {
        public static readonly MsgBroker Default  = new MsgBroker();

        private class Subscription : ISubscription
        {
            private readonly Delegate _action;
            private readonly MsgBroker _broker;
            private readonly WeakReference _subscriber;
            private readonly Type _type;
            private readonly bool _hasParameter;

            public SynchronizationContext SyncContext { get; set; }

            public Type ObjectType => _type;

            public Subscription(MsgBroker broker, object subscriber, Type type, Delegate action)
            {
                _broker = broker;
                _subscriber = subscriber != null ? new WeakReference(subscriber) : null;
                _type = type;
                _action = action;

                _hasParameter = (action.GetMethodInfo().GetParameters().Length == 1);
            }
            
            public void Dispose()
            {
                Unsubscribe();
            }

            public void Unsubscribe()
            {
                _broker.Unsubscribe(this);
            }

            public virtual bool IsMatch(string topic) => true;
            public virtual bool IsMatch(Type type, string topic) => (_type == null || (type != null && _type.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))) && IsMatch(topic);
            public bool IsDead() => _subscriber != null && !_subscriber.IsAlive;
            public bool MatchesSubscriber(object subscriber) => _subscriber != null && _subscriber.Target.Equals(subscriber);

            public void Call(object parameter)
            {
                if (_hasParameter)
                    _action.DynamicInvoke(parameter);
                else
                    _action.DynamicInvoke();
            }
        }

        private class NamedTopicSubscription : Subscription
        {
            public NamedTopicSubscription(MsgBroker broker, object subscriber, string name, Type type, Delegate action) : base(broker, subscriber, type, action)
            {
                Name = name;
            }

            public string Name { get; }

            public override bool IsMatch(string topic) => Name == null || Name == topic;
        }

        private class RegexTopicSubscription : Subscription
        {
            public RegexTopicSubscription(MsgBroker broker, object subscriber, Regex regex, Type type, Delegate action) : base(broker, subscriber, type, action)
            {
                Regex = regex;
            }

            public Regex Regex { get; }

            public override bool IsMatch(string topic) => Regex == null || (topic != null && Regex.IsMatch(topic));
        }

        private readonly object _lock = new object();
        private readonly List<Subscription> _subscriptions = new List<Subscription>();

        public void Publish(string topic, Type type, object data)
        {
            List<Subscription> subs;

            lock (_lock)
            {
                _subscriptions.RemoveAll(sub => sub.IsDead());

                subs = _subscriptions.Where(sub => sub.IsMatch(type, topic)).ToList();
            }

            foreach (var subscription in subs)
            {
                subscription.Call(data);
            }
        }

        public void PublishObject<T>(string topic, T data)
        {
            Publish(topic,typeof(T),data);
        }

        public void PublishObject<T>(T data)
        {
            Publish(null,typeof(T),data);
        }

        public void PublishObject(object data)
        {
            Publish(null, data?.GetType(), data);
        }

        public void PublishObject(string topic, object data)
        {
            Publish(topic, data?.GetType(), data);
        }

        public void Publish(string topic)
        {
            Publish(topic, null, null);
        }

        public ISubscription Subscribe<T>(object subscriber, Action<T> action)
        {
            return Subscribe(subscriber, typeof(T), action);
        }

        public ISubscription Subscribe<T>(object subscriber, string topic, Action<T> action)
        {
            return Subscribe(subscriber, topic, typeof(T), action);
        }

        public ISubscription Subscribe<T>(object subscriber, Regex regex, Action<T> action)
        {
            return Subscribe(subscriber, regex, typeof(T), action);
        }

        public ISubscription Subscribe(object subscriber, string topic, Action handler)
        {
            return Subscribe(subscriber, topic, null, handler);
        }

        public ISubscription Subscribe(object subscriber, Regex regex, Action handler)
        {
            return Subscribe(subscriber, regex, null, handler);
        }

        private Subscription AddSubscription(Subscription subscription)
        {
            lock (_lock)
            {
                _subscriptions.Add(subscription);
            }

            return subscription;
        }

        public ISubscription Subscribe(object subscriber, Type type, Delegate func)
        {
            return AddSubscription(new Subscription(this, subscriber, type, func));
        }

        public ISubscription Subscribe(object subscriber, string topic, Type type, Delegate func)
        {
            return AddSubscription(new NamedTopicSubscription(this, subscriber, topic, type, func));
        }

        public ISubscription Subscribe(object subscriber, Regex regex, Type type, Delegate func)
        {
            return AddSubscription(new RegexTopicSubscription(this, subscriber, regex, type, func));
        }

        public void Unsubscribe(ISubscription subscription)
        {
            lock (_lock)
            {
                _subscriptions.RemoveAll(sub => sub.IsDead());
                _subscriptions.Remove((Subscription)subscription);
            }
        }

        public void Unsubscribe(object subscriber)
        {
            Unsubscribe(sub => sub.MatchesSubscriber(subscriber));
        }

        public void Unsubscribe(object subscriber, Type type)
        {
            Unsubscribe(sub => sub.MatchesSubscriber(subscriber) && sub.ObjectType == type);
        }

        public void Unsubscribe(object subscriber, string topic)
        {
            Unsubscribe(sub => sub.MatchesSubscriber(subscriber) && sub is NamedTopicSubscription named && named.Name == topic);
        }

        public void Unsubscribe(object subscriber, string topic, Type type)
        {
            Unsubscribe(sub => sub.MatchesSubscriber(subscriber) && sub is NamedTopicSubscription named && topic == named.Name && sub.ObjectType == type);
        }

        public void Unsubscribe(object subscriber, Regex regex)
        {
            Unsubscribe(sub => sub.MatchesSubscriber(subscriber) && sub is RegexTopicSubscription regexSub && (regexSub.Regex == regex || regexSub.Regex.ToString() == regex.ToString()));
        }

        public void Unsubscribe(object subscriber, Regex regex, Type type)
        {
            Unsubscribe(sub => sub.MatchesSubscriber(subscriber) && sub is RegexTopicSubscription regexSub && sub.ObjectType == type && (regexSub.Regex == regex || regexSub.Regex.ToString() == regex.ToString()));
        }

        public void Unsubscribe<T>(object subscriber)
        {
            Unsubscribe(subscriber, typeof(T));
        }

        public void Unsubscribe<T>(object subscriber, string topic)
        {
            Unsubscribe(subscriber, topic, typeof(T));
        }

        public void Unsubscribe<T>(object subscriber, Regex regex)
        {
            Unsubscribe(subscriber, regex, typeof(T));
        }

        private void Unsubscribe(Predicate<Subscription> predicate)
        {
            lock (_lock)
            {
                _subscriptions.RemoveAll(sub => sub.IsDead());
                _subscriptions.RemoveAll(predicate);
            }
        }
    }
}