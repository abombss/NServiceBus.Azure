namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus.QueueAndTopicByEndpoint
{
    using System;
    using Config;

    internal static class NamingConventions
    {
        internal static Func<Configure, Type, string, string> QueueNamingConvention
        {
            get
            {
                return (config, messagetype, endpointname) =>
                {
                    var queueName = endpointname;

                    var configSection = config != null ? config.Settings.GetConfigSection<AzureServiceBusQueueConfig>() : null;

                    if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
                    {
                        queueName = configSection.QueueName;
                    }

                    if (queueName.Length >= 283) // 290 - a spot for the "-" & 6 digits for the individualizer
                        queueName = new DeterministicGuidBuilder().Build(queueName).ToString();

                    if (config != null && !config.Settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                        queueName = QueueIndividualizer.Individualize(queueName);

                    return queueName;
                };
            }
        }

        internal static Func<Configure, Type, string, string> SubscriptionNamingConvention
        {
            get
            {
                return (config, messagetype, endpointname) =>
                {
                    var subscriptionName = messagetype != null ? endpointname + "." + messagetype.Name : endpointname;

                    if (subscriptionName.Length >= 50)
                        subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

                    if (config != null && !config.Settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                        subscriptionName = QueueIndividualizer.Individualize(subscriptionName);

                    return subscriptionName;
                };
            }
        }

        internal static Func<Configure, Type, string, string> SubscriptionFullNamingConvention
        {
            get
            {
                return (config, messagetype, endpointname) =>
                {
                    var subscriptionName = messagetype != null ? endpointname + "." + messagetype.FullName : endpointname;

                    if (subscriptionName.Length >= 50)
                        subscriptionName = new DeterministicGuidBuilder().Build(subscriptionName).ToString();

                    if (config != null && !config.Settings.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
                        subscriptionName = QueueIndividualizer.Individualize(subscriptionName);

                    return subscriptionName;
                };
            }
        }

        internal static Func<Configure, Type, string, string> TopicNamingConvention
        {
            get
            {
                return (config, messagetype, endpointname) =>
                {
                    var name = endpointname;

                    if (name.Length >= 290)
                        name = new DeterministicGuidBuilder().Build(name).ToString();

                    return name;
                };
            }
        }

        internal static Func<Configure, Address, Address> PublisherAddressConvention
        {
            get
            {
                return (config, address) => Address.Parse(TopicNamingConvention(config, null, address.Queue + ".events") + "@" + address.Machine);
            }
        }

        internal static Func<Configure, Address, Address> PublisherAddressConventionForSubscriptions
        {
            get { return PublisherAddressConvention; }
        }

        internal static Func<Configure, Address, Address> QueueAddressConvention
        {
            get
            {
                return (config, address) => Address.Parse(QueueNamingConvention(config, null, address.Queue) + "@" + address.Machine);
            }
        }
    }
}