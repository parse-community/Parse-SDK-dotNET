using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

using Parse;

namespace ParseXamarinPushSample {
  // The UIApplicationDelegate for the application. This class is responsible for launching the 
  // User Interface of the application, as well as listening (and optionally responding) to 
  // application events from iOS.
  [Register("AppDelegate")]
  public partial class AppDelegate : UIApplicationDelegate {
    // class-level declarations
    UIWindow window;

    //
    // This method is invoked when the application has loaded and is ready to run. In this 
    // method you should instantiate the window, load the UI into it and then make the window
    // visible.
    //
    // You have 17 seconds to return from this method, or iOS will terminate your application.
    //
    public override bool FinishedLaunching(UIApplication app, NSDictionary options) {
      // Initialize the Parse client with your Application ID and Windows Key found on
      // your Parse dashboard
      ParseClient.Initialize("YOUR APPLICATION ID", "YOUR .NET KEY");

      // Register for remote notifications
      if (Convert.ToInt16(UIDevice.CurrentDevice.SystemVersion.Split('.')[0].ToString()) < 8) {
        UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound;
        UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
      } else {
        UIUserNotificationType notificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
        var settings = UIUserNotificationSettings.GetSettingsForTypes(notificationTypes, new NSSet(new string[] { }));
        UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
        UIApplication.SharedApplication.RegisterForRemoteNotifications();
      }

      // Handle Parse Push notification.
      ParsePush.ParsePushNotificationReceived += (object sender, ParsePushNotificationEventArgs args) => {
        Console.WriteLine("You received a notification!");
      };

      // create a new window instance based on the screen size
      window = new UIWindow(UIScreen.MainScreen.Bounds);

      UIViewController viewController = new UIViewController();
      UILabel label = new UILabel(new CoreGraphics.CGRect(0, 0, 160, 20));
      label.Text = "Parse Push is ready!";
      label.TextColor = UIColor.Gray;
      label.Center = viewController.View.Center;
      viewController.Add(label);
      window.BackgroundColor = UIColor.White;
      // If you have defined a view, add it here:
      window.RootViewController = viewController;
      window.MakeKeyAndVisible();

      return true;
    }

    public override void DidRegisterUserNotificationSettings(UIApplication application, UIUserNotificationSettings notificationSettings) {
      application.RegisterForRemoteNotifications();
    }

    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) {
      ParseInstallation installation = ParseInstallation.CurrentInstallation;
      installation.SetDeviceTokenFromData(deviceToken);

      installation.SaveAsync();
    }

    public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo) {
      // We need this to fire userInfo into ParsePushNotificationReceived.
      ParsePush.HandlePush(userInfo);
    }
  }
}