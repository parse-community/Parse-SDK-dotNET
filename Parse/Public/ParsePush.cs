// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Push.Internal;
using Parse.Core.Internal;
using Parse.Common.Internal;

namespace Parse
{
    /// <summary>
    ///  A utility class for sending and receiving push notifications.
    /// </summary>
    public partial class ParsePush
    {
        private object mutex;
        private IPushState state;

        /// <summary>
        /// Creates a push which will target every device. The Data field must be set before calling SendAsync.
        /// </summary>
        public ParsePush()
        {
            mutex = new object();
            // Default to everyone.
            state = new MutablePushState
            {
                Query = ParseInstallation.Query
            };
        }

        #region Properties

        /// <summary>
        /// An installation query that specifies which installations should receive
        /// this push.
        /// This should not be used in tandem with Channels.
        /// </summary>
        public ParseQuery<ParseInstallation> Query
        {
            get { return state.Query; }
            set
            {
                MutateState(s =>
                {
                    if (s.Channels != null && value != null && value.GetConstraint("channels") != null)
                    {
                        throw new InvalidOperationException("A push may not have both Channels and a Query with a channels constraint");
                    }
                    s.Query = value;
                });
            }
        }

        /// <summary>
        /// A short-hand to set a query which only discriminates on the channels to which a device is subscribed.
        /// This is shorthand for:
        ///
        /// <code>
        /// var push = new Push();
        /// push.Query = ParseInstallation.Query.WhereKeyContainedIn("channels", channels);
        /// </code>
        ///
        /// This cannot be used in tandem with Query.
        /// </summary>
        public IEnumerable<string> Channels
        {
            get { return state.Channels; }
            set
            {
                MutateState(s =>
                {
                    if (value != null && s.Query != null && s.Query.GetConstraint("channels") != null)
                    {
                        throw new InvalidOperationException("A push may not have both Channels and a Query with a channels constraint");
                    }
                    s.Channels = value;
                });
            }
        }

        /// <summary>
        /// The time at which this push will expire. This should not be used in tandem with ExpirationInterval.
        /// </summary>
        public DateTime? Expiration
        {
            get { return state.Expiration; }
            set
            {
                MutateState(s =>
                {
                    if (s.ExpirationInterval != null)
                    {
                        throw new InvalidOperationException("Cannot set Expiration after setting ExpirationInterval");
                    }
                    s.Expiration = value;
                });
            }
        }

        /// <summary>
        /// The time at which this push will be sent.
        /// </summary>
        public DateTime? PushTime
        {
            get { return state.PushTime; }
            set
            {
                MutateState(s =>
                {
                    DateTime now = DateTime.Now;
                    if (value < now || value > now.AddDays(14))
                    {
                        throw new InvalidOperationException("Cannot set PushTime in the past or more than two weeks later than now");
                    }
                    s.PushTime = value;
                });
            }
        }

        /// <summary>
        /// The time from initial schedul when this push will expire. This should not be used in tandem with Expiration.
        /// </summary>
        public TimeSpan? ExpirationInterval
        {
            get { return state.ExpirationInterval; }
            set
            {
                MutateState(s =>
                {
                    if (s.Expiration != null)
                    {
                        throw new InvalidOperationException("Cannot set ExpirationInterval after setting Expiration");
                    }
                    s.ExpirationInterval = value;
                });
            }
        }

        /// <summary>
        /// The contents of this push. Some keys have special meaning. A full list of pre-defined
        /// keys can be found in the Parse Push Guide. The following keys affect WinRT devices.
        /// Keys which do not start with x-winrt- can be prefixed with x-winrt- to specify an
        /// override only sent to winrt devices.
        /// alert: the body of the alert text.
        /// title: The title of the text.
        /// x-winrt-payload: A full XML payload to be sent to WinRT installations instead of
        ///      the auto-layout.
        /// This should not be used in tandem with Alert.
        /// </summary>
        public IDictionary<string, object> Data
        {
            get { return state.Data; }
            set
            {
                MutateState(s =>
                {
                    if (s.Alert != null && value != null)
                    {
                        throw new InvalidOperationException("A push may not have both an Alert and Data");
                    }
                    s.Data = value;
                });
            }
        }

        /// <summary>
        /// A convenience method which sets Data to a dictionary with alert as its only field. Equivalent to
        ///
        /// <code>
        /// Data = new Dictionary&lt;string, object&gt; {{"alert", alert}};
        /// </code>
        ///
        /// This should not be used in tandem with Data.
        /// </summary>
        public string Alert
        {
            get { return state.Alert; }
            set
            {
                MutateState(s =>
                {
                    if (s.Data != null && value != null)
                    {
                        throw new InvalidOperationException("A push may not have both an Alert and Data");
                    }
                    s.Alert = value;
                });
            }
        }

        #endregion

        internal IDictionary<string, object> Encode()
        {
            return ParsePushEncoder.Instance.Encode(state);
        }

        private void MutateState(Action<MutablePushState> func)
        {
            lock (mutex)
            {
                state = state.MutatedClone(func);
            }
        }

        private static IParsePushController PushController
        {
            get
            {
                return ParsePushPlugins.Instance.PushController;
            }
        }

        private static IParsePushChannelsController PushChannelsController
        {
            get
            {
                return ParsePushPlugins.Instance.PushChannelsController;
            }
        }

        #region Sending Push

        /// <summary>
        /// Request a push to be sent. When this task completes, Parse has successfully acknowledged a request
        /// to send push notifications but has not necessarily finished sending all notifications
        /// requested. The current status of recent push notifications can be seen in your Push Notifications
        /// console on http://parse.com
        /// </summary>
        /// <returns>A Task for continuation.</returns>
        public Task SendAsync()
        {
            return SendAsync(CancellationToken.None);
        }

        /// <summary>
        /// Request a push to be sent. When this task completes, Parse has successfully acknowledged a request
        /// to send push notifications but has not necessarily finished sending all notifications
        /// requested. The current status of recent push notifications can be seen in your Push Notifications
        /// console on http://parse.com
        /// </summary>
        /// <param name="cancellationToken">CancellationToken to cancel the current operation.</param>
        public Task SendAsync(CancellationToken cancellationToken)
        {
            return PushController.SendPushNotificationAsync(state, cancellationToken);
        }

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
        public static Task SendAlertAsync(string alert)
        {
            var push = new ParsePush();
            push.Alert = alert;
            return push.SendAsync();
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
        public static Task SendAlertAsync(string alert, string channel)
        {
            var push = new ParsePush();
            push.Channels = new List<string> { channel };
            push.Alert = alert;
            return push.SendAsync();
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
        public static Task SendAlertAsync(string alert, IEnumerable<string> channels)
        {
            var push = new ParsePush();
            push.Channels = channels;
            push.Alert = alert;
            return push.SendAsync();
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
        public static Task SendAlertAsync(string alert, ParseQuery<ParseInstallation> query)
        {
            var push = new ParsePush();
            push.Query = query;
            push.Alert = alert;
            return push.SendAsync();
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
        public static Task SendDataAsync(IDictionary<string, object> data)
        {
            var push = new ParsePush();
            push.Data = data;
            return push.SendAsync();
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
        public static Task SendDataAsync(IDictionary<string, object> data, string channel)
        {
            var push = new ParsePush();
            push.Channels = new List<string> { channel };
            push.Data = data;
            return push.SendAsync();
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
        public static Task SendDataAsync(IDictionary<string, object> data, IEnumerable<string> channels)
        {
            var push = new ParsePush();
            push.Channels = channels;
            push.Data = data;
            return push.SendAsync();
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
        public static Task SendDataAsync(IDictionary<string, object> data, ParseQuery<ParseInstallation> query)
        {
            var push = new ParsePush();
            push.Query = query;
            push.Data = data;
            return push.SendAsync();
        }

        #endregion

        #region Receiving Push

        /// <summary>
        /// An event fired when a push notification is received.
        /// </summary>
        public static event EventHandler<ParsePushNotificationEventArgs> ParsePushNotificationReceived
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

        internal static readonly SynchronizedEventHandler<ParsePushNotificationEventArgs> parsePushNotificationReceived = new SynchronizedEventHandler<ParsePushNotificationEventArgs>();

        #endregion

        #region Push Subscription

        /// <summary>
        /// Subscribe the current installation to this channel. This is shorthand for:
        ///
        /// <code>
        /// var installation = ParseInstallation.CurrentInstallation;
        /// installation.AddUniqueToList("channels", channel);
        /// installation.SaveAsync();
        /// </code>
        /// </summary>
        /// <param name="channel">The channel to which this installation should subscribe.</param>
        public static Task SubscribeAsync(string channel)
        {
            return SubscribeAsync(new List<string> { channel }, CancellationToken.None);
        }

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
        public static Task SubscribeAsync(string channel, CancellationToken cancellationToken)
        {
            return SubscribeAsync(new List<string> { channel }, cancellationToken);
        }

        /// <summary>
        /// Subscribe the current installation to these channels. This is shorthand for:
        ///
        /// <code>
        /// var installation = ParseInstallation.CurrentInstallation;
        /// installation.AddRangeUniqueToList("channels", channels);
        /// installation.SaveAsync();
        /// </code>
        /// </summary>
        /// <param name="channels">The channels to which this installation should subscribe.</param>
        public static Task SubscribeAsync(IEnumerable<string> channels)
        {
            return SubscribeAsync(channels, CancellationToken.None);
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
        public static Task SubscribeAsync(IEnumerable<string> channels, CancellationToken cancellationToken)
        {
            return PushChannelsController.SubscribeAsync(channels, cancellationToken);
        }

        /// <summary>
        /// Unsubscribe the current installation from this channel. This is shorthand for:
        ///
        /// <code>
        /// var installation = ParseInstallation.CurrentInstallation;
        /// installation.Remove("channels", channel);
        /// installation.SaveAsync();
        /// </code>
        /// </summary>
        /// <param name="channel">The channel from which this installation should unsubscribe.</param>
        public static Task UnsubscribeAsync(string channel)
        {
            return UnsubscribeAsync(new List<string> { channel }, CancellationToken.None);
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
        public static Task UnsubscribeAsync(string channel, CancellationToken cancellationToken)
        {
            return UnsubscribeAsync(new List<string> { channel }, cancellationToken);
        }

        /// <summary>
        /// Unsubscribe the current installation from these channels. This is shorthand for:
        ///
        /// <code>
        /// var installation = ParseInstallation.CurrentInstallation;
        /// installation.RemoveAllFromList("channels", channels);
        /// installation.SaveAsync();
        /// </code>
        /// </summary>
        /// <param name="channels">The channels from which this installation should unsubscribe.</param>
        public static Task UnsubscribeAsync(IEnumerable<string> channels)
        {
            return UnsubscribeAsync(channels, CancellationToken.None);
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
        public static Task UnsubscribeAsync(IEnumerable<string> channels, CancellationToken cancellationToken)
        {
            return PushChannelsController.UnsubscribeAsync(channels, cancellationToken);
        }

        #endregion
    }
}
