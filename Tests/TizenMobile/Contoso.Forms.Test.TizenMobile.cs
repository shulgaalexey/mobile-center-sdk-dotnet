using System;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using Microsoft.Azure.Mobile.Crashes.Ingestion.Models;

namespace Contoso.Forms.Test.TizenMobile
{
    class Program : global::Xamarin.Forms.Platform.Tizen.FormsApplication
    {
        protected override void OnCreate()
        {
            ErrorReportPageUpdateCallback.RequestUpdate = () =>
            {
                Crashes.GetLastSessionCrashReportAsync().ContinueWith(task =>
                {
                    ErrorReportPageUpdateCallback.Update(task.Result);
                });
            };

            base.OnCreate();
            MobileCenter.Configure("aae8b673-40f5-47b9-a688-30e42212cb02");
            LoadApplication(new App());
        }

        static void Main(string[] args)
        {
            var app = new Program();
            global::Xamarin.Forms.Platform.Tizen.Forms.Init(app);
            app.Run(args);
        }
    }
}