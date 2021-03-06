namespace NServiceBus.Hosting.Roles.Handlers
{
    using System.Linq;
    using Roles;
    using Transports;


    /// <summary>
    /// Configuring the right transport based on  <see cref="UsingTransport{T}"/> role on the endpoint config
    /// </summary>
    class TransportRoleHandler : IConfigureRole<UsingTransport<TransportDefinition>>
    {
        public void ConfigureRole(IConfigureThisEndpoint specifier, Configure config)
        {
            var transportDefinitionType =
                specifier.GetType()
                         .GetInterfaces()
                         .SelectMany(i => i.GetGenericArguments())
                         .Single(t => typeof(TransportDefinition).IsAssignableFrom(t));

            config.UseTransport(transportDefinitionType);
        }
    }
}