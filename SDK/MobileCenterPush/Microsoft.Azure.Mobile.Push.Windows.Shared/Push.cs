﻿using Microsoft.Azure.Mobile.Channel;
using Microsoft.Azure.Mobile.Ingestion.Models.Serialization;
using Microsoft.Azure.Mobile.Push.Ingestion.Models;
using Microsoft.Azure.Mobile.Utils.Synchronization;

namespace Microsoft.Azure.Mobile.Push
{
    public partial class Push : MobileCenterService
    {
        #region static
        private static readonly object PushLock = new object();

        private static Push _instanceField;

        public static Push Instance
        {
            get
            {
                lock (PushLock)
                {
                    return _instanceField ?? (_instanceField = new Push());
                }
            }
            set
            {
                lock (PushLock)
                {
                    _instanceField = value;
                }
            }
        }

        /// <summary>
        /// Push module enabled or disabled
        /// </summary>
        private static bool PlatformEnabled
        {
            get
            {
                lock (PushLock)
                {
                    return Instance.InstanceEnabled;
                }
            }
            set
            {
                lock (PushLock)
                {
                    Instance.InstanceEnabled = value;
                }
            }
        }

        #endregion

        #region instance

        private readonly StatefulMutex _mutex = new StatefulMutex();

        public override string ServiceName => "Push";

        protected override string ChannelName => "push";

        public Push()
        {
            LogSerializer.AddLogType(PushInstallationLog.JsonIdentifier, typeof(PushInstallationLog));
        }

        /// <summary>
        /// Method that is called to signal start of the Push service.
        /// </summary>
        /// <param name="channelGroup"></param>
        /// <param name="appSecret"></param>
        public override void OnChannelGroupReady(IChannelGroup channelGroup, string appSecret)
        {
            using (_mutex.GetLock())
            {
                base.OnChannelGroupReady(channelGroup, appSecret);
                ApplyEnabledState(Enabled);
            }
        }

        public override bool InstanceEnabled
        {
            get
            {
                return base.InstanceEnabled;
            }

            set
            {
                using (_mutex.GetLock())
                {
                    var prevValue = InstanceEnabled;
                    base.InstanceEnabled = value;
                    _mutex.InvalidateState();
                    if (value != prevValue)
                    {
                        ApplyEnabledState(value);
                    }
                }
            }
        }

        #endregion
    }
}
