﻿namespace NServiceBus.Features
{
    using Azure.Transports.WindowsAzureStorageQueues;
    using Config;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using ObjectBuilder;
    using Settings;
    using Transports;

    public class AzureStorageQueueTransport : ConfigureTransport<AzureStorageQueue>
    {
        protected override void InternalConfigure(Configure config)
        {
            Enable<AzureStorageQueueTransport>();
            EnableByDefault<MessageDrivenSubscriptions>();
            EnableByDefault<StorageDrivenPublisher>();
            EnableByDefault<TimeoutManager>();
            Categories.Serializers.SetDefault<JsonSerialization>();

            config.Settings.SetDefault("ScaleOut.UseSingleBrokerQueue", true); // default to one queue for all instances

            var configSection = config.GetConfigSection<AzureQueueConfig>();

            if(configSection == null)
                return;

            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.PurgeOnStartup, configSection.PurgeOnStartup);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, configSection.MaximumWaitTimeWhenIdle);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, configSection.MessageInvisibleTime);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.PeekInterval, configSection.PeekInterval);
            config.Settings.SetPropertyDefault<AzureMessageQueueReceiver>(t => t.BatchSize, configSection.BatchSize);
        }

        public override void Initialize(Configure config)
        {
            CloudQueueClient queueClient;

            var configSection = config.GetConfigSection<AzureQueueConfig>();

            var connectionString = TryGetConnectionString(configSection, config);

            if (string.IsNullOrEmpty(connectionString))
            {
                queueClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();     
            }
            else
            {
                queueClient = CloudStorageAccount.Parse(connectionString).CreateCloudQueueClient();

                Address.OverrideDefaultMachine(connectionString);                
            }

            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<CloudQueueClient>(queueClient);

            var recieverConfig = 
            NServiceBus.Configure.Component<AzureMessageQueueReceiver>(DependencyLifecycle.InstancePerCall);
            recieverConfig.ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);
            //todo: change all this
            //config.Configurer.ConfigureComponent()
                //.Component<AzureMessageQueueSender>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<PollingDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<AzureMessageQueueCreator>(DependencyLifecycle.InstancePerCall);

            var queuename = AzureQueueNamingConvention.Apply(NServiceBus.Configure.EndpointName);
            config.Settings.ApplyTo<AzureMessageQueueReceiver>((IComponentConfig)recieverConfig);
            Address.InitializeLocalAddress(queuename);
        }

        static string TryGetConnectionString(AzureQueueConfig configSection, Configure config)
        {
            var connectionString = config.Settings.Get<string>("NServiceBus.Transport.ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                if (configSection != null)
                {
                    connectionString = configSection.ConnectionString;
                }
            }

            return connectionString;
        }

        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "todo - refactor the transport to use a connection string instead of a custom section"; }
        }

    }
}