﻿using System;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Push;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;

namespace Contoso.UWP.Puppet
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            CoreApplication.EnablePrelaunch(true);
            InitializeComponent();
            Suspending += OnSuspending;
            MobileCenter.LogLevel = LogLevel.Verbose;
            MobileCenter.SetLogUrl("https://in-integration.dev.avalanch.es");
            MobileCenter.Start("42f4a839-c54c-44da-8072-a2f2a61751b2", typeof(Analytics), typeof(Crashes), typeof(Push));
            Push.PushNotificationReceived += PushNotificationReceivedHandler;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            BackgroundExecutionManager.RemoveAccess();
            BackgroundExecutionManager.RequestAccessAsync().AsTask().Wait();
            BGTask.RegisterBackgroundTask("", "task", new SystemTrigger(SystemTriggerType.InternetAvailable, false), new SystemCondition(SystemConditionType.InternetAvailable));

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
            Push.CheckLaunchedFromNotification(e);
        }

        private void PushNotificationReceivedHandler(object sender, PushNotificationReceivedEventArgs args)
        {
            string title = args.Title;
            string message = args.Message;
            var customData = args.CustomData;

            string customDataString = string.Empty;
            foreach (var pair in customData)
            {
                customDataString += $"key='{pair.Key}', value='{pair.Value}'";
            }

            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message))
            {
                MobileCenterLog.Debug(MobileCenterLog.LogTag, $"PushNotificationReceivedHandler received title:'{title}', message:'{message}', customData:{customDataString}");
            }
            else
            {
                MobileCenterLog.Debug(MobileCenterLog.LogTag, $"PushNotificationReceivedHandler received customData:{customDataString}");
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }


        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }

    public class BGTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
        }

        // Adapted from Microsoft documentation
        public static BackgroundTaskRegistration RegisterBackgroundTask(string taskEntryPoint,
            string taskName,
            IBackgroundTrigger trigger,
            IBackgroundCondition condition)
        {
            // Check for existing registrations of this background task.
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == taskName)
                {
                    // The task is already registered.
                    return (BackgroundTaskRegistration)cur.Value;
                }
            }

            // Register the background task.
            var builder = new BackgroundTaskBuilder {Name = taskName};
            builder.SetTrigger(trigger);
            if (condition != null)
            {
                builder.AddCondition(condition);
            }
            var task = builder.Register();
            return task;
        }
    }
}
