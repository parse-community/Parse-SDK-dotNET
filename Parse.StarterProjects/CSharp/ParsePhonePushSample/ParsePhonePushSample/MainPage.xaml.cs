using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using ParsePhonePushSample.Resources;
using Parse;
using Microsoft.Phone.Notification;

namespace ParsePhonePushSample {
  public partial class MainPage : PhoneApplicationPage {
    // Constructor
    public MainPage() {
      ParsePush.ToastNotificationReceived += (sender, args) => {
        string title = null;
        string alert = null;
        args.Collection.TryGetValue("wp:Text1", out title);
        args.Collection.TryGetValue("wp:Text2", out alert);
        Status.Text += "\nReceived push.";
        if (title != null) {
          Status.Text += " Title: " + title;
        }
        if (alert != null) {
          Status.Text += " Alert: " + alert;
        }
      };

      InitializeComponent();

      // Sample code to localize the ApplicationBar
      //BuildLocalizedApplicationBar();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e) {
      var installation = ParseInstallation.CurrentInstallation;
      Status.Text += "Saving the current installation";
      await installation.SaveAsync();
      Status.Text += "\nDone saving the current installation";
      if (!installation.ContainsKey("deviceUris")) {
        Status.Text += "\nThe ParseInstallation does not have a push channel. " +
          "Please be sure you have the ID_CAP_PUSH_NOTIFICATION capability";
      } else if (installation.Get<IDictionary<string, object>>("deviceUris").ContainsKey("_Toast")) {
        Status.Text += "\nYour device is ready to receive toast notifications";
      }
    }

    private async void Send_Click(object sender, RoutedEventArgs e) {
      Status.Text += "\nSending push to all subscribers...";
      await ParsePush.SendDataAsync(new Dictionary<string, object> {
        {"title", PushTitle.Text},
        {"alert", PushAlert.Text},
      });
      Status.Text += "\nDone sending push.";
    }
  }
}