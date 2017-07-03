﻿using Com.Microsoft.Azure.Mobile.Crashes.Ingestion.Models;

namespace Microsoft.Azure.Mobile.Crashes
{
    public partial class ErrorAttachmentLog
    {
        internal AndroidErrorAttachmentLog internalAttachment { get; }

        ErrorAttachmentLog(AndroidErrorAttachmentLog androidAttachment)
        {
            internalAttachment = androidAttachment;
        }

        static ErrorAttachmentLog PlatformAttachmentWithText(string text, string fileName)
        {
            AndroidErrorAttachmentLog androidAttachment = AndroidErrorAttachmentLog.AttachmentWithText(text, fileName);
            return new ErrorAttachmentLog(androidAttachment);
        }

        static ErrorAttachmentLog PlatformAttachmentWithBinary(byte[] data, string filename, string contentType)
        {
            AndroidErrorAttachmentLog androidAttachment = AndroidErrorAttachmentLog.AttachmentWithBinary(data, filename, contentType);
            return new ErrorAttachmentLog(androidAttachment);
        }
    }
}
