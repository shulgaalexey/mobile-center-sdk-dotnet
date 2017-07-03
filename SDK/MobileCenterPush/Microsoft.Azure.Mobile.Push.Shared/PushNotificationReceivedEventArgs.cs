﻿using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Mobile.Push
{
    /// <summary>
    /// Event args for event that occurs when a push notification is received.
    /// </summary>
    /// <seealso cref="Push.PushNotificationReceived"/>
    public class PushNotificationReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Custom data attached to the push message.
        /// </summary>
        public IDictionary<string, string> CustomData;

        /// <summary>
        /// Title of the push message.
        /// </summary>
        public string Title;

        /// <summary>
        /// Body of the push message
        /// </summary>
        public string Message;
    }
}
