﻿using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Router;

class PreroutingToSubscribePreroutingFork : ChainTerminator<PreroutingContext>
{
    protected override async Task<bool> Terminate(PreroutingContext context)
    {
        if (context.Intent == MessageIntentEnum.Subscribe
            || context.Intent == MessageIntentEnum.Unsubscribe)
        {
            var messageTypeString = GetSubscriptionMessageTypeFrom(context);

            if (string.IsNullOrEmpty(messageTypeString))
            {
                throw new UnforwardableMessageException("The subscription message type header is missing.");
            }

            if (!context.Headers.TryGetValue(Headers.SubscriberTransportAddress, out var subscriberAddress))
            {
                subscriberAddress = GetReplyToAddress(context);
            }

            context.Headers.TryGetValue(Headers.SubscriberEndpoint, out var subscriberEndpoint);

            if (subscriberEndpoint == null && subscriberAddress == null)
            {
                throw new UnforwardableMessageException("Either subscriber address or subscriber endpoint (or both) are required in a subscription message.");
            }

            if (context.Intent == MessageIntentEnum.Subscribe)
            {
                await context.Chains.Get<SubscribePreroutingContext>()
                    .Invoke(new SubscribePreroutingContext(context, messageTypeString, subscriberEndpoint, subscriberAddress))
                    .ConfigureAwait(false);
            }
            else
            {
                await context.Chains.Get<UnsubscribePreroutingContext>()
                    .Invoke(new UnsubscribePreroutingContext(context, messageTypeString, subscriberEndpoint, subscriberAddress))
                    .ConfigureAwait(false);
            }

            return true;
        }

        return false;
    }

    static string GetReplyToAddress(PreroutingContext message)
    {
        return message.Headers.TryGetValue(Headers.ReplyToAddress, out var replyToAddress) ? replyToAddress : null;
    }

    static string GetSubscriptionMessageTypeFrom(PreroutingContext msg)
    {
        msg.Headers.TryGetValue(Headers.SubscriptionMessageType, out var value);
        return value;
    }
}


