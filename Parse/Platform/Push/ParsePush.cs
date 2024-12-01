using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Push;
using Parse.Platform.Push;

namespace Parse
{
    /// <summary>
    ///  A utility class for sending and receiving push notifications.
    /// </summary>
    public partial class ParsePush
    {
        object Mutex { get; } = new object { };

        IPushState State { get; set; }

        IServiceHub Services { get; }

#pragma warning disable CS1030 // #warning directive
#warning Make default(IServiceHub) the default value of serviceHub once all dependents properly inject it.

        /// <summary>
        /// Creates a push which will target every device. The Data field must be set before calling SendAsync.
        /// </summary>
        public ParsePush(IServiceHub serviceHub)
#pragma warning restore CS1030 // #warning directive
        {
            Services = serviceHub ?? ParseClient.Instance;
            State = new MutablePushState { Query = Services.GetInstallationQuery() };
        }

        #region Properties

        /// <summary>
        /// An installation query that specifies which installations should receive
        /// this push.
        /// This should not be used in tandem with Channels.
        /// </summary>
        public ParseQuery<ParseInstallation> Query
        {
            get => State.Query;
            set => MutateState(state =>
            {
                if (state.Channels is { } && value is { } && value.GetConstraint("channels") is { })
                {
                    throw new InvalidOperationException("A push may not have both Channels and a Query with a channels constraint.");
                }

                state.Query = value;
            });
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
            get => State.Channels;
            set => MutateState(state =>
            {
                if (value is { } && state.Query is { } && state.Query.GetConstraint("channels") is { })
                {
                    throw new InvalidOperationException("A push may not have both Channels and a Query with a channels constraint.");
                }

                state.Channels = value;
            });
        }

        /// <summary>
        /// The time at which this push will expire. This should not be used in tandem with ExpirationInterval.
        /// </summary>
        public DateTime? Expiration
        {
            get => State.Expiration;
            set => MutateState(state =>
            {
                if (state.ExpirationInterval is { })
                {
                    throw new InvalidOperationException("Cannot set Expiration after setting ExpirationInterval.");
                }

                state.Expiration = value;
            });
        }

        /// <summary>
        /// The time at which this push will be sent.
        /// </summary>
        public DateTime? PushTime
        {
            get => State.PushTime;
            set => MutateState(state =>
            {
                DateTime now = DateTime.Now;

                if (value < now || value > now.AddDays(14))
                {
                    throw new InvalidOperationException("Cannot set PushTime in the past or more than two weeks later than now.");
                }

                state.PushTime = value;
            });
        }

        /// <summary>
        /// The time from initial schedul when this push will expire. This should not be used in tandem with Expiration.
        /// </summary>
        public TimeSpan? ExpirationInterval
        {
            get => State.ExpirationInterval;
            set => MutateState(state =>
            {
                if (state.Expiration is { })
                {
                    throw new InvalidOperationException("Cannot set ExpirationInterval after setting Expiration.");
                }

                state.ExpirationInterval = value;
            });
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
            get => State.Data;
            set => MutateState(state =>
            {
                if (state.Alert is { } && value is { })
                {
                    throw new InvalidOperationException("A push may not have both an Alert and Data.");
                }

                state.Data = value;
            });
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
            get => State.Alert;
            set => MutateState(state =>
            {
                if (state.Data is { } && value is { })
                {
                    throw new InvalidOperationException("A push may not have both an Alert and Data.");
                }

                state.Alert = value;
            });
        }

        #endregion

        internal IDictionary<string, object> Encode()
        {
            return ParsePushEncoder.Instance.Encode(State);
        }

        void MutateState(Action<MutablePushState> func)
        {
            lock (Mutex)
            {
                State = State.MutatedClone(func);
            }
        }

        #region Sending Push

        /// <summary>
        /// Request a push to be sent. When this task completes, Parse has successfully acknowledged a request
        /// to send push notifications but has not necessarily finished sending all notifications
        /// requested. The current status of recent push notifications can be seen in your Push Notifications
        /// console.
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
        /// console.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken to cancel the current operation.</param>
        public Task SendAsync(CancellationToken cancellationToken)
        {
            return Services.PushController.SendPushNotificationAsync(State, Services, cancellationToken);
        }

        #endregion
    }
}
