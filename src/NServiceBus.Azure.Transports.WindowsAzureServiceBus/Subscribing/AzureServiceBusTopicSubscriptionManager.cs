using System;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using MessageInterfaces;
    using NServiceBus.Transports;
    using Unicast;
    using Unicast.Routing;
    using Unicast.Transport;

    internal class AzureServiceBusTopicSubscriptionManager : IManageSubscriptions
    {
        readonly Configure config;
        readonly ITopology topology;

       

        public IMessageMapper MessageMapper { get; set; }
        public StaticMessageRouter MessageRouter { get; set; }

        public AzureServiceBusTopicSubscriptionManager(Configure config, ITopology topology)
        {
            this.config = config;
            this.topology = topology;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="original"></param>
        public void Subscribe(Type eventType, Address original)
        {
            if (original == null)
            {
                var addresses = GetAddressesForMessageType(eventType);
                foreach (var destination in addresses)
                {
                    SubscribeInternal(eventType, destination);
                }
            }
            else
            {
                SubscribeInternal(eventType, original);
            }
        }

        void SubscribeInternal(Type eventType, Address original)
        {
            // resolving manually as the bus also gets the subscription manager injected
            // but this is the only way to get to the correct dequeue strategy
            var bus = config.Builder.Build<UnicastBus>();
            var transport = bus.Transport as TransportReceiver;
            if (transport == null)
            {
                throw new Exception(
                    "AzureServiceBusTopicSubscriptionManager can only be used in conjunction with windows azure servicebus, please configure the windows azure servicebus transport");
            }
            var strategy = transport.Receiver as AzureServiceBusDequeueStrategy;
            if (strategy == null)
            {
                throw new Exception(
                    "AzureServiceBusTopicSubscriptionManager can only be used in conjunction with windows azure servicebus, please configure the windows azure servicebus transport");
            }

            var notifier = topology.Subscribe(eventType, original);
            strategy.TrackNotifier(eventType, original, notifier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="original"></param>
        public void Unsubscribe(Type eventType, Address original)
        {
            if (original == null)
            {
                var addresses = GetAddressesForMessageType(eventType);
                foreach (var destination in addresses)
                {
                    UnSubscribeInternal(eventType, destination);
                }
            }
            else
            {
                UnSubscribeInternal(eventType, original);
            }
        }

        void UnSubscribeInternal(Type eventType, Address original)
        {
            // resolving manually as the bus also gets the subscription manager injected
            // but this is the only way to get to the correct dequeue strategy
            var bus = config.Builder.Build<UnicastBus>();
            var transport = bus.Transport as TransportReceiver;
            if (transport == null)
            {
                throw new Exception(
                    "AzureServiceBusTopicSubscriptionManager can only be used in conjunction with windows azure servicebus, please configure the windows azure servicebus transport");
            }
            var strategy = transport.Receiver as AzureServiceBusDequeueStrategy;
            if (strategy == null)
            {
                throw new Exception(
                    "AzureServiceBusTopicSubscriptionManager can only be used in conjunction with windows azure servicebus, please configure the windows azure servicebus transport");
            }

            var notifier = strategy.GetNotifier(eventType, original);

            topology.Unsubscribe(notifier);

            strategy.RemoveNotifier(eventType, original);
        }

        /// <summary>
        /// Gets the destination address For a message type.
        /// </summary>
        /// <param name="messageType">The message type to get the destination for.</param>
        /// <returns>The address of the destination associated with the message type.</returns>
        List<Address> GetAddressesForMessageType(Type messageType)
        {
            var destinations = MessageRouter.GetDestinationFor(messageType);

            if (destinations.Any())
            {
                return destinations;
            }

            if (MessageMapper != null && !messageType.IsInterface)
            {
                var t = MessageMapper.GetMappedTypeFor(messageType);
                if (t != null && t != messageType)
                {
                    return GetAddressesForMessageType(t);
                }
            }

            return destinations;
        }
    }
}