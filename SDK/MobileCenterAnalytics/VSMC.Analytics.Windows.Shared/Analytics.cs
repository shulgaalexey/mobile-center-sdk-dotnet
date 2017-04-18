﻿using System;
using System.Collections.Generic;
using Microsoft.Azure.Mobile.Channel;
using Microsoft.Azure.Mobile.Analytics.Ingestion.Models;
using Microsoft.Azure.Mobile.Ingestion.Models;
using Microsoft.Azure.Mobile.Analytics.Channel;
using Microsoft.Azure.Mobile.Utils;

namespace Microsoft.Azure.Mobile.Analytics
{
    public class Analytics : MobileCenterService
    {
        #region static

        private const int MaxEventProperties = 5;
        private const int MaxEventNameLength = 256;
        private const int MaxEventPropertyKeyLength = 64;
        private const int MaxEventPropertyValueLength = 64;

        private static readonly object AnalyticsLock = new object();

        private static Analytics _instanceField;

        public static Analytics Instance
        {
            get
            {
                lock (AnalyticsLock)
                {
                    return _instanceField ?? (_instanceField = new Analytics());
                }
            }
            set
            {
                lock (AnalyticsLock)
                {
                    _instanceField = value; //for testing
                }
            }
        }

        /// <summary>
        ///     Enable or disable Analytics module.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                lock (AnalyticsLock)
                {
                    return Instance.InstanceEnabled;
                }
            }
            set
            {
                lock (AnalyticsLock)
                {
                    Instance.InstanceEnabled = value;
                }
            }
        }

        /// <summary>
        ///     Track a custom event with name and optional properties.
        /// </summary>
        /// <remarks>
        ///     The name parameter can not be null or empty.Maximum allowed length = 256.
        ///     The properties parameter maximum item count = 5.
        ///     The properties keys/names can not be null or empty, maximum allowed key length = 64.
        ///     The properties values can not be null, maximum allowed value length = 64.
        /// </remarks>
        /// <param name="name">An event name.</param>
        /// <param name="properties">Optional properties.</param>
        public static void TrackEvent(string name, IDictionary<string, string> properties = null)
        {
            lock (AnalyticsLock)
            {
                Instance.InstanceTrackEvent(name, properties);
            }
        }

        #endregion

        #region instance

        /* Internal for testing purposes */
        internal ISessionTracker SessionTracker;
        internal readonly IApplicationLifecycleHelper ApplicationLifecycleHelper = new ApplicationLifecycleHelper();
        private readonly ISessionTrackerFactory _sessionTrackerFactory;
        private bool _hasResumed;

        internal Analytics()
        {
            LogSerializer.AddLogType(PageLog.JsonIdentifier, typeof(PageLog));
            LogSerializer.AddLogType(EventLog.JsonIdentifier, typeof(EventLog));
            LogSerializer.AddLogType(StartSessionLog.JsonIdentifier, typeof(StartSessionLog));
        }

        internal Analytics(ISessionTrackerFactory sessionTrackerFactory, IApplicationLifecycleHelper lifecycleHelper) : this()
        {
            _sessionTrackerFactory = sessionTrackerFactory;
            ApplicationLifecycleHelper = lifecycleHelper;
        }

        public override bool InstanceEnabled
        {
            get { return base.InstanceEnabled; }
            set
            {
                var prevValue = InstanceEnabled;
                base.InstanceEnabled = value;
                if (value != prevValue)
                {
                    ApplyEnabledState(value);
                }
            }
        }

        protected override string ChannelName => "analytics";

        public override string ServiceName => "Analytics";

        private void InstanceTrackEvent(string name, IDictionary<string, string> properties = null)
        {
            if (IsInactive)
            {
                return;
            }
            const string type = "Event";
            if (ValidateName(name, type))
            {
                properties = ValidateProperties(properties, name, type);
                var log = new EventLog(0, null, Guid.NewGuid(), name, null, properties);
                Channel.Enqueue(log);
            }
        }

        public override void OnChannelGroupReady(IChannelGroup channelGroup)
        {
            base.OnChannelGroupReady(channelGroup);
            ApplyEnabledState(InstanceEnabled);
            ApplicationLifecycleHelper.ApplicationResuming += (sender, e) =>
            {
                SessionTracker?.Resume();
                _hasResumed = true;
            };
            ApplicationLifecycleHelper.ApplicationSuspended += (sender, e) => SessionTracker?.Pause();
        }

        private void ApplyEnabledState(bool enabled)
        {
            if (enabled && ChannelGroup != null && SessionTracker == null)
            {
                SessionTracker = CreateSessionTracker(ChannelGroup, Channel);
                ApplicationLifecycleHelper.Enabled = true;
                if (_hasResumed)
                {
                    SessionTracker.Resume();
                }
            }
            else if (!enabled)
            {
                ApplicationLifecycleHelper.Enabled = false;
                SessionTracker?.ClearSessions();
                SessionTracker = null;
            }
        }

        private ISessionTracker CreateSessionTracker(IChannelGroup channelGroup, IChannelUnit channel)
        {
            return _sessionTrackerFactory?.CreateSessionTracker(channelGroup, channel) ?? new SessionTracker(channelGroup, channel);
        }

        /// <summary>
        /// Validates name.
        /// </summary>
        /// <param name="name">Log name to validate.</param>
        /// <param name="logType">Log type.</param>
        /// <returns><c>true</c> if validation succeeds, otherwise <с>false</с>.</returns>
        private bool ValidateName(string name, string logType)
        {
            if (string.IsNullOrEmpty(name))
            {
                MobileCenterLog.Error(LogTag, logType + " name cannot be null or empty.");
                return false;
            }
            if (name.Length > MaxEventNameLength)
            {
                MobileCenterLog.Error(LogTag, string.Format("{0} '{1}' : name length cannot be longer than {2} characters.", logType, name, MaxEventNameLength));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates properties.
        /// </summary>
        /// <param name="properties">Properties collection to validate.</param>
        /// <param name="logName">Log name.</param>
        /// <param name="logType">Log type.</param>
        /// <returns>Valid properties collection with maximum size of 5</returns>
        private IDictionary<string, string> ValidateProperties(IDictionary<string, string> properties, string logName, string logType)
        {
            if (properties == null)
            {
                return null;
            }
            var result = new Dictionary<string, string>();
            foreach (var property in properties)
            {
                if (result.Count >= MaxEventProperties)
                {
                    MobileCenterLog.Warn(LogTag, string.Format("{0} '{1}' : properties cannot contain more than {2} items. Skipping other properties.", logType, logName, MaxEventProperties));
                    break;
                }
                if (string.IsNullOrEmpty(property.Key))
                {
                    MobileCenterLog.Warn(LogTag, string.Format("{0} '{1}' : a property key cannot be null or empty. Property will be skipped.", logType, logName));
                }
                else if (property.Key.Length > MaxEventPropertyKeyLength)
                {
                    MobileCenterLog.Warn(LogTag, string.Format("{0} '{1}' : property '{2}' : property key length cannot be longer than {3} characters. Property '{2}' will be skipped.", logType, logName, property.Key, MaxEventPropertyKeyLength));
                }
                else if(property.Value == null)
                {
                    MobileCenterLog.Warn(LogTag, string.Format("{0} '{1}' : property '{2}' : property value cannot be null. Property '{2}' will be skipped.", logType, logName, property.Key));
                }
                else if(property.Value.Length > MaxEventPropertyValueLength)
                {
                    MobileCenterLog.Warn(LogTag, string.Format("{0} '{1}' : property '{2}' : property value cannot be longer than {3} characters. Property '{2}' will be skipped.", logType, logName, property.Key, MaxEventPropertyValueLength));
                }
                else
                {
                    result.Add(property.Key, property.Value);
                }
            }
            return result;
        }

        #endregion
    }
}
