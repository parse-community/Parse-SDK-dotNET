using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Parse;

namespace ParseXamarinPushSample {
  [Application(Name = "parsexamarinpushsample.ParseApplication")]
  class ParseApplication : Application {
    public ParseApplication(IntPtr handle, JniHandleOwnership ownerShip)
      : base(handle, ownerShip) {
    }

    public override void OnCreate() {
      base.OnCreate();

      ParseClient.Initialize("YOUR APPLICATION ID", "YOUR .NET KEY");
      ParsePush.ParsePushNotificationReceived += ParsePush.DefaultParsePushNotificationReceivedHandler;
    }
  }
}