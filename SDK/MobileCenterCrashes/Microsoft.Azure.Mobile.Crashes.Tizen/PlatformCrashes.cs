﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Azure.Mobile.Crashes
{
    public class TestCrashException : Exception { }

    class PlatformCrashes : PlatformCrashesBase
    {
        private const string WatsonKey = "VSMCAppSecret";
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int WerRegisterCustomMetadata([MarshalAs(UnmanagedType.LPWStr)]string key, [MarshalAs(UnmanagedType.LPWStr)]string value);

        /// <exception cref="MobileCenterException"/>
        public void Configure(string appSecret)
        {
            try
            {
                WerRegisterCustomMetadata(WatsonKey, appSecret);
            }
            catch (Exception e)
            {
                throw new MobileCenterException("Failed to register crashes with Watson", e);
            }
        }

        // Note: in PlatformCrashes we use only callbacks; not events (in Crashes, there are corresponding events)
        public override SendingErrorReportEventHandler SendingErrorReport { get; set; }
        public override SentErrorReportEventHandler SentErrorReport { get; set; }
        public override FailedToSendErrorReportEventHandler FailedToSendErrorReport { get; set; }
        public override ShouldProcessErrorReportCallback ShouldProcessErrorReport { get; set; }
        //public override GetErrorAttachmentCallback GetErrorAttachment { get; set; }
        public override ShouldAwaitUserConfirmationCallback ShouldAwaitUserConfirmation { get; set; }

        public override void NotifyUserConfirmation(UserConfirmation confirmation)
        {
        }
        public override bool Enabled { get; set; }
        public override bool HasCrashedInLastSession => false;

        public override Type BindingType => typeof(Crashes);

        public override async Task<ErrorReport> GetLastSessionCrashReportAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    throw new NotImplementedException();
                }
                catch (Exception e)
                {
                    MobileCenterLog.Error(MobileCenterLog.LogTag, "GetLastSessionCrashReportAsync(0) exception: " + e.GetType() + "\n" + e.Message);
                    throw new NotImplementedException();
                }
            });
            return null;
        }
    }
}