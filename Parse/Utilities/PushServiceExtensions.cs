using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;
using Parse.Platform.Push;

namespace Parse
{
    public static class PushServiceExtensions
    {
        /// <summary>
        /// Pushes a simple message to every device. This is shorthand for:
        ///
        /// <code>
        /// var push = new ParsePush();
        /// push.Data = new Dictionary&lt;string, object&gt;{{"alert", alert}};
        /// return push.SendAsync();
        /// </code>
        /// </summary>
        /// <param name="alert">The alert message to send.</param>
        public static Task SendPushAlertAsync(this IServiceHub serviceHub, string alert)
        {
            return new ParsePush(serviceHub) { Alert = alert }.SendAsync();
        }

        /// <summary>
        /// Pushes a simple message to every device subscribed to channel. This is shorthand for:
        ///
        /// <code>
        /// var push = new ParsePush();
        /// push.Channels = new List&lt;string&gt; { channel };
        /// push.Data = new Dictionary&lt;string, object&gt;{{"alert", alert}};
        /// return push.SendAsync();
        /// </code>
        /// </summary>
        /// <param name="alert">The alert message to send.</param>
        /// <param name="channel">An Installation must be subscribed to channel to receive this Push Notification.</param>
        public static Task SendPushAlertAsync(this IServiceHub serviceHub, string alert, string channel)
        {
            return new ParsePush(serviceHub) { Channels = new List<string> { channel }, Alert = alert }.SendAsync();
        }

        /// <summary>
        /// Pushes a simple message to every device subscribed to any of channels. This is shorthand for:
        ///
        /// <code>
        /// var push = new ParsePush();
        /// push.Channels = channels;
        /// push.Data = new Dictionary&lt;string, object&gt;{{"alert", alert}};
        /// return push.SendAsync();
        /// </code>
        /// </summary>
        /// <param name="alert">The alert message to send.</param>
        /// <param name="channels">An Installation must be subscribed to any of channels to receive this Push Notification.</param>
        public static Task SendPushAlertAsync(this IServiceHub serviceHub, string alert, IEnumerable<string> channels)
        {
            return new ParsePush(serviceHub) { Channels = channels, Alert = alert }.SendAsync();
        }

        /// <summary>
        /// Pushes a simple message to every device matching the target query. This is shorthand for:
        ///
        /// <code>
        /// var push = new ParsePush();
        /// push.Query = query;
        /// push.Data = new Dictionary&lt;string, object&gt;{{"alert", alert}};
        /// return push.SendAsync();
        /// </code>
        /// </summary>
        /// <param name="alert">The alert message to send.</param>
        /// <param name="query">A query filtering the devices which should receive this Push Notification.</param>
        public static Task SendPushAlertAsync(this IServiceHub serviceHub, string alert, ParseQuery<ParseInstallation> query)
        {
            return new ParsePush(serviceHub) { Query = query, Alert = alert }.SendAsync();
        }

        /// <summary>
        /// Pushes an arbitrary payload to every device. This is shorthand for:
        ///
        /// <code>
        /// var push = new ParsePush();
        /// push.Data = data;
        /// return push.SendAsync();
        /// </code>
        /// </summary>
        /// <param name="data">A push payload. See the ParsePush.Data property for more information.</param>
        public static Task SendPushDataAsync(this IServiceHub serviceHub, IDictionary<string, object> data)
        {
            return new ParsePush(serviceHub) { Data = data }.SendAsync();
        }

        /// <summary>
        /// Pushes an arbitrary payload to every device subscribed to channel. This is shorthand for:
        ///
        /// <code>
        /// var push = new ParsePush();
        /// push.Channels = new List&lt;string&gt; { channel };
        /// push.Data = data;
        /// return push.SendAsync();
        /// </code>
        /// </summary>
        /// <param name="data">A push payload. See the ParsePush.Data property for more information.</param>
        /// <param name="channel">An Installation must be subscribed to channel to receive this Push Notification.</param>
        public static Task SendPushDataAsync(this IServiceHub serviceHub, IDictionary<string, object> data, string channel)
        {
            return new ParsePush(serviceHub) { Channels = new List<string> { channel }, Data = data }.SendAsync();
        }

        /// <summary>
        /// Pushes an arbitrary payload to every device subscribed to any of channels. This is shorthand for:
        ///
        /// <code>
        /// var push = new ParsePush();
        /// push.Channels = channels;
        /// push.Data = data;
        /// return push.SendAsync();
        /// </code>
        /// </summary>
        /// <param name="data">A push payload. See the ParsePush.Data property for more information.</param>
        /// <param name="channels">An Installation must be subscribed to any of channels to receive this Push Notification.</param>
        public static Task SendPushDataAsync(this IServiceHub serviceHub, IDictionary<string, object> data, IEnumerable<string> channels)
        {
            return new ParsePush(serviceHub) { Channels = channels, Data = data }.SendAsync();
        }

        /// <summary>
        /// Pushes an arbitrary payload to every device matching target. This is shorthand for:
        ///
        /// <code>
        /// var push = new ParsePush();
        /// push.Query = query
        /// push.Data = data;
        /// return push.SendAsync();
        /// </code>
        /// </summary>
        /// <param name="data">A push payload. See the ParsePush.Data property for more information.</param>
        /// <param name="query">A query filtering the devices which should receive this Push Notification.</param>
        public static Task SendPushDataAsync(this IServiceHub serviceHub, IDictionary<string, object> data, ParseQuery<ParseInstallation> query)
        {
            return new ParsePush(serviceHub) { Query = query, Data = data }.SendAsync();
        }

        #region Receiving Push

#pragma warning disable CS1030 // #warning directive
#warning Check if this should be moved into IParsePushController.

        /// <summary>
        /// An event fired when a push notification is received.
        /// </summary>
        public static event EventHandler<ParsePushNotificationEvent> ParsePushNotificationReceived
#pragma warning restore CS1030 // #warning directive
        {
            add
            {
                parsePushNotificationReceived.Add(value);
            }
            remove
            {
                parsePushNotificationReceived.Remove(value);
            }
        }

        internal static readonly SynchronizedEventHandler<ParsePushNotificationEvent> parsePushNotificationReceived = new SynchronizedEventHandler<ParsePushNotificationEvent>();

        #endregion

        #region Push Subscription

        /// <summary>
        /// Subscribe the current installation to this channel. This is shorthand for:
        ///
        /// <code>
        /// var installation = ParseInstallation.CurrentInstallation;
        /// installation.AddUniqueToList("channels", channel);
        /// installation.SaveAsync(cancellationToken);
        /// </code>
        /// </summary>
        /// <param name="channel">The channel to which this installation should subscribe.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the current operation.</param>
        public static Task SubscribeToPushChannelAsync(this IServiceHub serviceHub, string channel, CancellationToken cancellationToken = default)
        {
            return SubscribeToPushChannelsAsync(serviceHub, new List<string> { channel }, cancellationToken);
        }

        /// <summary>
        /// Subscribe the current installation to these channels. This is shorthand for:
        ///
        /// <code>
        /// var installation = ParseInstallation.CurrentInstallation;
        /// installation.AddRangeUniqueToList("channels", channels);
        /// installation.SaveAsync(cancellationToken);
        /// </code>
        /// </summary>
        /// <param name="channels">The channels to which this installation should subscribe.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the current operation.</param>
        public static Task SubscribeToPushChannelsAsync(this IServiceHub serviceHub, IEnumerable<string> channels, CancellationToken cancellationToken = default)
        {
            return serviceHub.PushChannelsController.SubscribeAsync(channels, serviceHub, cancellationToken);
        }

        /// <summary>
        /// Unsubscribe the current installation from this channel. This is shorthand for:
        ///
        /// <code>
        /// var installation = ParseInstallation.CurrentInstallation;
        /// installation.Remove("channels", channel);
        /// installation.SaveAsync(cancellationToken);
        /// </code>
        /// </summary>
        /// <param name="channel">The channel from which this installation should unsubscribe.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the current operation.</param>
        public static Task UnsubscribeToPushChannelAsync(this IServiceHub serviceHub, string channel, CancellationToken cancellationToken = default)
        {
            return UnsubscribeToPushChannelsAsync(serviceHub, new List<string> { channel }, cancellationToken);
        }

        /// <summary>
        /// Unsubscribe the current installation from these channels. This is shorthand for:
        ///
        /// <code>
        /// var installation = ParseInstallation.CurrentInstallation;
        /// installation.RemoveAllFromList("channels", channels);
        /// installation.SaveAsync(cancellationToken);
        /// </code>
        /// </summary>
        /// <param name="channels">The channels from which this installation should unsubscribe.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the current operation.</param>
        public static Task UnsubscribeToPushChannelsAsync(this IServiceHub serviceHub, IEnumerable<string> channels, CancellationToken cancellationToken = default)
        {
            return serviceHub.PushChannelsController.UnsubscribeAsync(channels, serviceHub, cancellationToken);
        }

        #endregion
    }
}
