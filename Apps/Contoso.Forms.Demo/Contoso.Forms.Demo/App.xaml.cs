﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using Microsoft.Azure.Mobile.Distribute;
using Microsoft.Azure.Mobile.Push;
using Xamarin.Forms;

namespace Contoso.Forms.Demo
{
    public partial class App : Application
    {
        const string uwpKey = "5bce20c8-f00b-49ca-8580-7a49d5705d4c";
        const string androidKey = "987b5941-4fac-4968-933e-98a7ff29237c";
        const string iosKey = "fe2bf05d-f4f9-48a6-83d9-ea8033fbb644";

        static App()
        {
            Push.PushNotificationReceived += OnPushNotificationReceived;
        }

        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainDemoPage());
        }

        protected override void OnStart()
        {
            MobileCenter.LogLevel = LogLevel.Verbose;
            Crashes.GetErrorAttachments = GetErrorAttachments;
            Distribute.ReleaseAvailable = OnReleaseAvailable;
            MobileCenter.Start($"uwp={uwpKey};android={androidKey};ios={iosKey}",
                               typeof(Analytics), typeof(Crashes), typeof(Distribute), typeof(Push));
        }


        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        static void OnPushNotificationReceived(object sender, PushNotificationReceivedEventArgs e)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                var customData = "{" + string.Join(",", e.CustomData.Select(kv => kv.Key + "=" + kv.Value)) + "}";
                Current.MainPage.DisplayAlert(e.Title, e.Message + "\nCustom data=" + customData, "OK");
            });
        }

        IEnumerable<ErrorAttachmentLog> GetErrorAttachments(ErrorReport report)
        {
            return new ErrorAttachmentLog[]
            {
                ErrorAttachmentLog.AttachmentWithText("Hello world!", "hello.txt"),
                ErrorAttachmentLog.AttachmentWithBinary(Encoding.UTF8.GetBytes("Fake image"), "fake_image.jpeg", "image/jpeg")
            };
        }

        bool OnReleaseAvailable(ReleaseDetails releaseDetails)
        {
            MobileCenterLog.Info("MobileCenterDemo", "OnReleaseAvailable id=" + releaseDetails.Id
                                            + " version=" + releaseDetails.Version
                                            + " releaseNotesUrl=" + releaseDetails.ReleaseNotesUrl);
            var custom = releaseDetails.ReleaseNotes?.ToLowerInvariant().Contains("custom") ?? false;
            if (custom)
            {
                var title = "Version " + releaseDetails.ShortVersion + " available!";
                Task answer;
                if (releaseDetails.MandatoryUpdate)
                {
                    answer = Current.MainPage.DisplayAlert(title, releaseDetails.ReleaseNotes, "Update now!");
                }
                else
                {
                    answer = Current.MainPage.DisplayAlert(title, releaseDetails.ReleaseNotes, "Update now!", "Maybe tomorrow...");
                }
                answer.ContinueWith((task) =>
                {
                    if (releaseDetails.MandatoryUpdate || (task as Task<bool>).Result)
                    {
                        Distribute.NotifyUpdateAction(UpdateAction.Update);
                    }
                    else
                    {
                        Distribute.NotifyUpdateAction(UpdateAction.Postpone);
                    }
                });
            }
            return custom;
        }
    }
}
