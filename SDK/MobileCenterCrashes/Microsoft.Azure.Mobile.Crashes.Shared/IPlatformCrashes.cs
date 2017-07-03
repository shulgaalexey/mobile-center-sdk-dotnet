﻿using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Mobile.Crashes
{
    /// <summary>
    /// Interface to abstract <see cref="Crashes"/> features between different platforms.
    /// </summary>
    internal interface IPlatformCrashes
    {
        Type BindingType { get; }

        bool Enabled { get; set; }

        bool HasCrashedInLastSession { get; }

        Task<ErrorReport> GetLastSessionCrashReportAsync();

        void GenerateTestCrash();

        void NotifyUserConfirmation(UserConfirmation confirmation);

        // Note: in PlatformCrashes we use only callbacks; not events (in Crashes, there are corresponding events)
        SendingErrorReportEventHandler SendingErrorReport { get; set; }
        SentErrorReportEventHandler SentErrorReport { get; set; }
        FailedToSendErrorReportEventHandler FailedToSendErrorReport { get; set; }
        ShouldProcessErrorReportCallback ShouldProcessErrorReport { get; set; }
        ShouldAwaitUserConfirmationCallback ShouldAwaitUserConfirmation { get; set; }
        GetErrorAttachmentsCallback GetErrorAttachments { get; set; }
        //void TrackException(Exception exception);
    }
}
