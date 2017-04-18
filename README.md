[![GitHub Release](https://img.shields.io/github/release/Microsoft/mobile-center-sdk-xamarin.svg)](https://github.com/Microsoft/mobile-center-sdk-xamarin/releases/latest)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Mobile.svg)](https://www.nuget.org/packages/Microsoft.Azure.Mobile/)
[![license](https://img.shields.io/badge/license-MIT%20License-yellow.svg)](https://github.com/Microsoft/mobile-center-sdk-xamarin/blob/master/license.txt)

# Mobile Center SDK for Xamarin

## Introduction

The Microsoft Mobile Center SDKs allow you to add Mobile Center services to your mobile application.

The SDK currently supports the following services:

1. **Analytics**: Mobile Center Analytics helps you understand user behavior and customer engagement to improve your Android app. The SDK automatically captures session count and device properties like model, OS Version etc. You can define your own custom events to measure things that matter to your business. All the information captured is available in the Mobile Center portal for you to analyze the data.

2. **Crashes**: Mobile Center Crashes will automatically generate a crash log every time your app crashes. The log is first written to the device's storage and on the next app start, the crash report is forwarded to Mobile Center. Collecting crashes works for both beta and live apps, i.e. those submitted to the App Store, Google Play or other app stores. Crash logs contain valuable information for you to help resolve the issue. The SDK gives you a lot of flexibility how to handle a crash log. As a developer you can collect and add additional information to the report if you like.

3. **Distribute**: Mobile Center Distribute enables your users to install a new version of the app when you distribute it via Mobile Center. With a new version of the app available, the SDK will present an update dialog to the users to either download, postpone or ignore the latest version. Once they tap **Download**, the SDK will update the application. If necessary, you can force your users to update the app to continue to use it. Note that this feature will `NOT` work if your app is deployed to the app store, if you are developing locally, if you are distributing new versions through a channel other than mobile center or if it is a debug build of your app (for Android Apps only).

This document contains the following sections:

1. [Prerequisites](#1-prerequisites)
2. [Supported Platforms](#2-supported-platforms)
3. [Setup](#3-setup)
4. [Start the SDK](#4-start-the-sdk)
5. [Analytics APIs](#5-analytics-apis)
6. [Crashes APIs](#6-crashes-apis)
7. [Distribute APIs](#7-distribute-apis)
8. [Advanced APIs](#8-advanced-apis)
9. [Contributing](#9-contributing)
10. [Contact](#10-contact)

Let's get started with setting up Mobile Center Xamarin SDK in your app to use these services:

## 1. Prerequisites

Before you begin, please make sure that the following prerequisites are met:

* A project setup in Xamarin Studio or Xamarin for Visual Studio.
* You are not using other crash services on the same mobile app for iOS platform.

## 2. Supported Platforms 

We support the following platforms:

* Xamarin.iOS   
   Choose this option if you want to use Xamarin only in iOS application. You need to create one app in Mobile Center portal with OS as `iOS` and platform as `Xamarin`.
* Xamarin.Android  
   Choose this option if you want to use Xamarin only in Android application. You need to create one app in Mobile Center portal with OS as `Android` and platform as `Xamarin`.
* Xamarin.Forms (iOS and Android)  
   Choose this option if you want to create cross platform app in iOS and Android. You need to create two apps in Mobile Center - one for each OS and select platform as `Xamarin`.

## 3. Setup

Mobile Center SDK is designed with a modular approach – a developer only needs to integrate the modules of the services that they're interested in. If you'd like to get started with just Analytics or Crashes, include their packages in your app. For each iOS, Android and Forms project, add the 'Mobile Center Analytics' and 'Mobile Center Crashes' packages.

## For Xamarin Studio ##

**For Xamarin.iOS and Xamarin.Android:**  
* Navigate to the Project -> 'Add NuGet Packages...'
* Search for 'Mobile Center' and select "Mobile Center Analytics" and "Mobile Center Crashes". Then Click 'Add Packages'  

**For Xamarin.Forms**  
Multiplatform Xamarin.Forms app has three projects in your solution - portable class library or shared library, project.Droid, project.iOS . You need to add NuGet packages to each of these projects.

* Navigate to the Project -> 'Add NuGet Packages...'
* Search for 'Mobile Center' and select "Mobile Center Analytics" and "Mobile Center Crashes". Then Click 'Add Packages'

## For Xamarin for Visual Studio ##

* Navigate Project -> Manage NuGet Packages...
* Search for 'Mobile Center' and select "Mobile Center Analytics" and "Mobile Center Crashes". Then Click 'Add Packages'

## Using Package Manager Console ##

* Make sure Package Manager Console is opened in either Xamarin Studio or Visual Studio. You will have to install an add-in for Xamarin Studio.
* Type the following commands:  
   PM> Install-Package Microsoft.Azure.Mobile.Analytics   
   PM> Install-Package Microsoft.Azure.Mobile.Crashes
  
Now that you've integrated the SDK in your application, it's time to start the SDK and make use of Mobile Center services.

**Note:** If you installed the "Mobile Center Crashes" package for iOS prior to version 0.3.0, your iOS project should contain the folder
"MobileCenterFrameworks" and its contents. It is no longer required and safe to delete.

## 4. Start the SDK

To start the SDK in your app, follow these steps:

1. **Add using statements:**  Add the appropriate namespaces before you get started with using our APIs.

    **Xamarin.iOS** -  Open AppDelegate.cs file and add the lines below the existing using statements

    **Xamarin.Android** - Open MainActivity.cs file and add the lines below the existing using statements

    **Xamarin.Forms** - Open App.xaml.cs file in your shared project and add these using statements 

    ```Xamarin
    using Microsoft.Azure.Mobile;
    using Microsoft.Azure.Mobile.Analytics;
    using Microsoft.Azure.Mobile.Crashes;
    using Microsoft.Azure.Mobile.Distribute;
    ```

2. **Start the SDK:**  Mobile Center provides developers with three modules to get started – Analytics, Crashes and Distribute. In order to use these modules, you need to opt in for the module(s) that you'd like, meaning by default no module is started and you will have to explicitly call each of them when starting the SDK.   

    **Xamarin.iOS**
 
   1. Open your `AppDelegate.cs` file and add the `Start`-API in your `FinishedLaunching`-method

    ```csharp
    Distribute.DontCheckForUpdatesInDebug();
    MobileCenter.Start("{Your Xamarin iOS App Secret}", typeof(Analytics), typeof(Crashes), typeof(Distribute));
    ```
    
    2. Add a new URL scheme to your `info.plist`. Open your `Info.plist` and switch to the **Advanced** tab. Copy and paste your bundle identifier as the `URL Identifier`, e.g. `com.example.awesomeapp`.
    3. Next, in the **Advanced** tab, enter `mobilecenter-${APP_SECRET}` as the URL scheme and replace `${APP_SECRET}` with the App Secret of your app.
    4. Implement the `openURL`-callback in your `AppDelegate` to enable in-app-updates and add the `Distribute.OpenUrl(url)`-call.
    
	```csharp
	public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
   {
   		Distribute.OpenUrl(url);

       return true;
   }
	```

    **Xamarin.Android**
    
    Open MainActivity.cs file and add the `Start` API in OnCreate() method

    ```csharp
    MobileCenter.Start("{Your Xamarin Android App Secret}", typeof(Analytics), typeof(Crashes), typeof(Distribute));
    ```

    **Xamarin.Forms**
    
   For creating a Xamarin.Forms application targeting both iOS and Android platforms, you need to create two applications in Mobile Center portal - one for each platform. Creating two apps will give you two App secrets - one for iOS and another for Android. Open the `App.xaml.cs` file (or your class that inherits `Xamarin.Forms.Application`) in your shared or portable project and add the API below in the `OnStart()` override method.

    ```csharp
    MobileCenter.Start("ios={Your Xamarin iOS App Secret};android={Your Xamarin Android App secret}",typeof(Analytics), typeof(Crashes), typeof(Distribute));
    ```
     
    You need to copy paste the App secret value for Xamarin iOS and Android app from Mobile Center portal. Make sure to replace the placeholder text above with the actual values for your application.
    
    The example above shows how to use the `Start()` method and include both the Analytics and Crashes module. If you wish not to use Analytics, remove the parameter from the method call above. Note that, unless you explicitly specify each module as parameters in the start method, you can't use that Mobile Center service. Also, the `Start()` API can be used only once in the lifecycle of your app – all other calls will log a warning to the console and only the modules included in the first call will be available.
    
   **Note that your Xamarin.iOS app requires additional setup steps for in-app updates to work.**

	In case you are using Mobile Center Distribute, make sure to add the `Distribute.DontCheckForUpdatesInDebug();` call to your `FinishedLaunching` method on you app delegate. It should look similar to this:
  	
	```csharp
    public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
    {
    	Xamarin.Forms.Forms.Init();
    	Distribute.DontCheckForUpdatesInDebug();
    	LoadApplication(new App());
    	return base.FinishedLaunching(uiApplication, launchOptions);
        } 
   ```
     
	Implement the `openURL`-callback in your `AppDelegate` to enable in-app-updates and add the `Distribute.OpenUrl(url)`-call.
    
	```csharp
	public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
   {
   		Distribute.OpenUrl(url);

       return true;
   }
	```


## 5. Analytics APIs

* **Track Session, Device Properties:**  Once the Analytics module is included in your app and the SDK is started, it will automatically track sessions, device properties like OS Version, model, manufacturer etc. and you don’t need to add any additional code.
    Look at the section above on how to [Start the SDK](#4-start-the-sdk) if you haven't started it yet.

* **Custom Events:** You can track your own custom events with specific properties to know what's happening in your app, understand user actions, and see the aggregates in the Mobile Center portal. Once you have started the SDK, use the `TrackEvent()` method to track your events with properties.

    ```csharp
    Analytics.TrackEvent("Video clicked", new Dictionary<string, string> { { "Category", "Music" }, { "FileName", "favorite.avi"}});
    ```

* **Enable or disable Analytics:**  You can change the enabled state of the Analytics module at runtime by calling the `Analytics.Enabled` property. If you disable it, the SDK will not collect any more analytics information for the app. To re-enable it, set property value as `true`.

    ```csharp
    Analytics.Enabled = true;
    ```
    You can also check if the module is enabled or not using:

    ```csharp
    bool isEnabled = Analytics.Enabled;
    ```

## 6. Crashes APIs

Once you set up and start the Mobile Center SDK to use the Crashes module in your application, the SDK will automatically start logging any crashes in the device's local storage. When the user opens the application again, all pending crash logs will automatically be forwarded to Mobile Center and you can analyze the crash along with the stack trace on the Mobile Center portal. Refer to the section to [Start the SDK](#4-start-the-sdk) if you haven't done so already.

* **Generate a test crash:** The SDK provides you with a static API to generate a test crash for easy testing of the SDK:

    ```csharp
    Crashes.GenerateTestCrash();
    ```

    Note that this API checks for debug vs release configurations. So you can only use it when debuging as it won't work for release apps.

* **Did the app crash in last session:** At any time after starting the SDK, you can check if the app crashed in the previous session:

    ```csharp
    bool didAppCrash = Crashes.HasCrashedInLastSession;
    ```

* **Enable or disable the Crashes module:**  You can disable and opt out of using the Crashes module by setting the `Enabled` property to `false` and the SDK will collect no crashes for your app. Use the same API to re-enable it by setting property as `true`.

    ```csharp
    Crashes.Enabled = true;
    ```

    You can also check whether the module is enabled or not using:

    ```csharp
    bool isEnabled = Crashes.Enabled;
    ```

* **Details about the last crash:** If your app crashed previously, you can get details about the last crash:

    ```csharp
    Crashes.GetLastSessionCrashReportAsync().ContinueWith(task =>
    {
        var errorReport = task.Result;
        // inspect errorReport, can be null
    });
    ```

* **Advanced Scenarios:**  The Crashes service provides events and callbacks for developers to perform additional actions before and when sending crash reports to Mobile Center. This gives you added flexibility on the crash reports that will be sent.
Note that the events must be subscribed to and callbacks must be set before Mobile Center is started.

    The following callbacks are provided:

    * **Should the crash be processed:** Set this callback if you'd like to decide if a particular crash needs to be processed or not. For example - there could be some system level crashes that you'd want to ignore and don't want to send to Mobile Center.

        ```csharp
        Crashes.ShouldProcessErrorReport = (report) =>
        {
                return true; // return true if the crash report should be processed, otherwise false.
        };
        ```

    * **User Confirmation:** By default the SDK automatically sends crash reports to Mobile Center. However, the SDK exposes a callback where you can tell it to await user confirmation before sending any crash reports.
    Your app is then responsible for obtaining confirmation, e.g. through a alert with one of these options - "Always Send", "Send", and "Don't Send". Based on the user input, you will tell the SDK and the crash will then respectively be forwarded to Mobile Center or not.

        ```csharp
        Crashes.ShouldAwaitUserConfirmation = () =>
        {
            return true; // Return true if the SDK should await user confirmation, otherwise false.
        };
        ```

        If you return `true`, your app should obtain user permission and message the SDK with the result using the following API:

        ```csharp
        Crashes.NotifyUserConfirmation(UserConfirmation confirmation);
        ```
        Pass one of `UserConfirmation.Send`, `UserConfirmation.DontSend` or `UserConfirmation.AlwaysSend`.

    The following events are provided:

    * **Before sending a crash report:** This callback will be invoked just before the crash is sent to Mobile Center:

        ```csharp
        Crashes.SendingErrorReport += (sender, e) =>
        {
        	...
        };

        ```

    * **When sending a crash report succeeded:** This callback will be invoked after sending a crash report succeeded:

        ```csharp
        Crashes.SentErrorReport += (sender, e) =>
        {
        	...
        };

        ```

    * **When sending a crash report failed:** This callback will be invoked after sending a crash report failed:

        ```csharp
        Crashes.FailedToSendErrorReport += (sender, e) =>
        {
        	...
        };

        ```
## 7. Distribute APIs

You can easily let your users get the latest version of your app by integrating Mobile Center Distribute. All you need to do is pass the service name as a parameter in the `Start()` API call. Once the app launches, the SDK checks for new updates in the background. If it finds a new update, users will see a dialog with three options - **Download**, **Postpone** and **Ignore**. If the user taps **Download**, Mobile Center Distribute will trigger the new version to be installed. **Postpone** will delay the download until the app is opened again. **Ignore** will not prompt the user again for that particular app version. In case of a required update, the user will only have the option to tap **Download**.

Please follow the paragraph in [Start the SDK](#3-start-the-sdk) to setup the Distribute service.

### Localization of the update UI

You can easily provide your own resource strings if you'd like to localize the text displayed in the update dialog. Look at the string files for iOS [in this resource file](https://github.com/Microsoft/mobile-center-sdk-ios/blob/develop/MobileCenterDistribute/MobileCenterDistribute/Resources/en.lproj/MobileCenterDistribute.strings) and those for Android [in this resource file](https://github.com/Microsoft/mobile-center-sdk-android/blob/distribute/sdk/mobile-center-distribute/src/main/res/values/strings.xml).

Use the same string name and specify the localized value to be reflected in the dialog in your own app resource files.  

### Enable or disable Distribute

You can change the enabled state by changing the `Enabled` property. If you disable it, the SDK will not prompt your users when a new version is available for install. To re-enable it, set the property to `true`.

Note that it will only disable SDK features for the Distribute service (in-app updates for your application) and the SDK API has nothing to do with disabling the **Distribute** service on the Mobile Center portal.

```csharp
Distribute.Enabled = false;
```

You can also check if the service is enabled in the SDK or not at runtime. 
  
```csharp
bool enabled = Distribute.Enabled;
```


## 8. Advanced APIs

* **Debugging**: You can control the amount of log messages that show up from the SDK. Use the API below to enable additional logging while debugging. By default, it is set it to `ASSERT` for non-debuggable applications and `WARN` for debuggable applications.

    ```csharp
        MobileCenter.LogLevel = LogLevel.Verbose;
    ```

* **Get Install Identifier**: The Mobile Center SDK creates a UUID for each device once the app is installed. This identifier remains the same for a device when the app is updated and a new one is generated only when the app is re-installed. The following API is useful for debugging purposes.

    ```csharp
        System.Guid installId = MobileCenter.InstallId;
    ```

* **Enable/Disable Mobile Center SDK:** If you want the Mobile Center SDK to be disabled completely, use the `Enabled` property. When disabled, the SDK will not forward any information to MobileCenter.

    ```csharp
        MobileCenter.Enabled = false;
    ```
    
## 9. Contributing

We're looking forward to your contributions via pull requests.

### 9.1 Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact opencode@microsoft.com with any additional questions or comments.

### 9.2 Contributor License

You must sign a [Contributor License Agreement](https://cla.microsoft.com/) before submitting your pull request. To complete the Contributor License Agreement (CLA), you will need to submit a request via the [form](https://cla.microsoft.com/) and then electronically sign the CLA when you receive the email containing the link to the document. You need to sign the CLA only once to cover submission to any Microsoft OSS project. 

## 10. Contact
If you have further questions or are running into trouble that cannot be resolved by any of the steps here, feel free to open a Github issue here or contact us at mobilecentersdk@microsoft.com.
