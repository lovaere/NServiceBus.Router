﻿using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Router;

class SendPreroutingTerminator : ChainTerminator<SendPreroutingContext>
{
    public SendPreroutingTerminator(IRoutingProtocol routingProtocol)
    {
        this.routingProtocol = routingProtocol;
    }
    protected override async Task<bool> Terminate(SendPreroutingContext context)
    {
        if (!context.Destinations.Any())
        {
            return false;
        }

        var outgoingInterfaces = routingProtocol.RouteTable.GetOutgoingInterfaces(context.IncomingInterface, context.Destinations)
            .ToArray();
        var routes = routingProtocol.RouteTable.Route(context.IncomingInterface, context.Destinations);

        var interfaces = context.Extensions.Get<IInterfaceChains>();
        var forkTasks = outgoingInterfaces
            .Select(iface =>
            {
                var chains = interfaces.GetChainsFor(iface);
                var chain = chains.Get<ForwardSendContext>();
                return chain.Invoke(new ForwardSendContext(iface, routes.ToArray(), context));
            });

        await Task.WhenAll(forkTasks).ConfigureAwait(false);

        return true;
    }

    IRoutingProtocol routingProtocol;
}