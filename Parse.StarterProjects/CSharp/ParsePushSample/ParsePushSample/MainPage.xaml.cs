using Parse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ParseStarterProject
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            SignupForPushAsync();
        }

        private async void SignupForPushAsync() {
          txtStatus.Text = "Saving Installation...";
          ParsePush.PushNotificationReceived += (sender, args) => {
            txtStatus.Text += "\nReceived push notification!";
          };

          await App.saveInstallationTask;
          txtStatus.Text += "\nDone saving Installation"; 
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached. The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void SendPush(object sender, RoutedEventArgs e) {
          var alert = txtAlert.Text;
          var title = txtTitle.Text;
          if (title.Length == 0) {
            txtStatus.Text += "\nSending push to everyone with alert: " + alert;
            ParsePush.SendAlertAsync(alert, from installation in ParseInstallation.Query
                                            where installation.DeviceType == "winrt"
                                            select installation);
            ParsePush.SendAlertAsync(alert);
          } else {
            txtStatus.Text += "\nSending push to everyone with title: " + title + ", alert: " + alert;
            ParsePush.SendDataAsync(new Dictionary<string, object>{
              {"title", title},
              {"alert", alert}
            }, from installation in ParseInstallation.Query
                                            where installation.DeviceType == "winrt"
                                            select installation);
          }
        }
    }
}
