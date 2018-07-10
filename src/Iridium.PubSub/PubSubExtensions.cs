using System;
using System.Text.RegularExpressions;

namespace Iridium.PubSub.Extensions
{
    public static class PubSubExtensions
    {
        public static ISubscription Subscribe<T>(this object subscriber, Action<T> action)
        {
            return MsgBroker.Default.Subscribe(subscriber, action);
        }

        public static ISubscription Subscribe<T>(this object subscriber, string name, Action<T> action)
        {
            return MsgBroker.Default.Subscribe(subscriber, name, action);
        }

        public static ISubscription Subscribe<T>(this object subscriber, Regex regex, Action<T> action)
        {
            return MsgBroker.Default.Subscribe(subscriber, regex, action);
        }

        public static ISubscription Subscribe(this object subscriber, string name, Action action)
        {
            return MsgBroker.Default.Subscribe(subscriber, name, action);
        }

        public static ISubscription Subscribe(this object subscriber, Regex regex, Action action)
        {
            return MsgBroker.Default.Subscribe(subscriber, regex, action);
        }

        public static ISubscription Subscribe(this object subscriber, Type type, Delegate func)
        {
            return MsgBroker.Default.Subscribe(subscriber, type, func);
        }

        public static ISubscription Subscribe(object subscriber, string name, Type type, Delegate func)
        {
            return MsgBroker.Default.Subscribe(subscriber, name, type, func);
        }

        public static ISubscription Subscribe(this object subscriber, Regex regex, Type type, Delegate func)
        {
            return MsgBroker.Default.Subscribe(subscriber, regex, type, func);
        }

        public static void Unsubscribe(this object subscriber)
        {
            MsgBroker.Default.Unsubscribe(subscriber);
        }

        public static void Unsubscribe(this object subscriber, Type type)
        {
            MsgBroker.Default.Unsubscribe(subscriber, type);
        }

        public static void Unsubscribe(this object subscriber, string name)
        {
            MsgBroker.Default.Unsubscribe(subscriber, name);
        }

        public static void Unsubscribe(this object subscriber, string name, Type type)
        {
            MsgBroker.Default.Unsubscribe(subscriber, name, type);
        }

        public static void Unsubscribe(this object subscriber, Regex regex)
        {
            MsgBroker.Default.Unsubscribe(subscriber, regex);
        }

        public static void Unsubscribe(this object subscriber, Regex regex, Type type)
        {
            MsgBroker.Default.Unsubscribe(subscriber, regex, type);
        }

        public static void Unsubscribe<T>(this object subscriber)
        {
            MsgBroker.Default.Unsubscribe<T>(subscriber);
        }

        public static void Unsubscribe<T>(this object subscriber, string name)
        {
            MsgBroker.Default.Unsubscribe<T>(subscriber, name);
        }

        public static void Unsubscribe<T>(this object subscriber, Regex regex)
        {
            MsgBroker.Default.Unsubscribe<T>(subscriber, regex);
        }
    }
}