﻿using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Mobile.Crashes
{
    /// <summary>
    /// Object used to share portable code between platforms.
    /// </summary>
    abstract class PlatformCrashesBase : IPlatformCrashes
    {
        public abstract Type BindingType { get; }

        public abstract bool Enabled { get; set; }

        public abstract bool HasCrashedInLastSession { get; }

        public abstract Task<ErrorReport> GetLastSessionCrashReportAsync();

        public void GenerateTestCrash()
        {
#if WINDOWS_UWP
#else
            throw new TestCrashException();
#endif
        }

        public abstract void NotifyUserConfirmation(UserConfirmation confirmation);

        // Note: in PlatformCrashes we use only callbacks; not events (in Crashes, there are some corresponding events)
        public abstract SendingErrorReportEventHandler SendingErrorReport { get; set; }
        public abstract SentErrorReportEventHandler SentErrorReport { get; set; }
        public abstract FailedToSendErrorReportEventHandler FailedToSendErrorReport { get; set; }
        public abstract ShouldProcessErrorReportCallback ShouldProcessErrorReport { get; set; }
        public abstract ShouldAwaitUserConfirmationCallback ShouldAwaitUserConfirmation { get; set; }
        public abstract GetErrorAttachmentsCallback GetErrorAttachments { get; set; }
        //public abstract void TrackException(Exception exception);
    }
}
