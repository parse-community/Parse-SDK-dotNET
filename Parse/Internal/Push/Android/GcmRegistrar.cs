// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using Android.App;
using Android.Content;
using Android.OS;
using System.Threading.Tasks;

namespace Parse {
  internal class GcmRegistrar {
    private const string LogTag = "parse.GcmRegistrar";

    private const string ExtraRegistrationId = "registration_id";

    private const string ExtraSenderId = "com.parse.push.gcm_sender_id";
    private const string ParseGcmSenderId = "1076345567071";

    public const string IntentRegisterAction = "com.google.android.c2dm.intent.REGISTER";

    private readonly Object mutex = new Object();
    private Request request;
    private Context context;

    public static GcmRegistrar GetInstance() {
      return Singleton.Instance;
    }

    private static class Singleton {
      public static readonly GcmRegistrar Instance = new GcmRegistrar(Application.Context);
    }

    private GcmRegistrar(Context context) {
      this.context = context;
    }

    private string getActualSenderIdFromExtra(Object senderIdExtra) {
      if (!(senderIdExtra is string)) {
        return null;
      }
      string senderId = senderIdExtra as string;
      if (!senderId.StartsWith("id:")) {
        return null;
      }

      return senderId.Substring(3);
    }

    public void Register() {
      ParseInstallation installation = ParseInstallation.CurrentInstallation;

      lock (mutex) {
        if (installation.DeviceToken == null && request == null) {
          var metadata = ManifestInfo.GetApplicationMetaData();
          object senderIdExtra = null;
          if (metadata != null) {
            senderIdExtra = metadata.Get(ExtraSenderId);
          }

          string senderIds = ParseGcmSenderId;
          if (senderIdExtra != null) {
            string senderId = getActualSenderIdFromExtra(senderIdExtra);

            if (senderId != null) {
              senderIds += "," + senderIdExtra;
            } else {
              Android.Util.Log.Error("parse.GcmRegistrar", "Found " + ExtraSenderId + " <meta-data> element with value \""
                + senderIdExtra.ToString() + "\", but the value is missing the expected \"id:\" prefix");
            }
          }
          request = Request.CreateAndSend(this.context, senderIds);
        }
      }
    }

    /// <summary>
    /// Handles GCM registration intent from <see cref="ParsePushBroadcastReceiver"/> and saves the GCM registration
    /// id as <see cref="ParseInstallation.CurrentInstallation"/> device token.
    /// </summary>
    /// <remarks>
    /// Should be called by a broadcast receiver or service to handle GCM registration response
    /// intent (com.google.android.c2dm.intent.REGISTRATION).
    /// </remarks>
    /// <param name="intent"></param>
    public Task HandleRegistrationIntentAsync(Intent intent) {
      if (intent.Action == ParsePushBroadcastReceiver.ActionGcmRegisterResponse) {
        string registrationId = intent.GetStringExtra(ExtraRegistrationId);

        if (registrationId != null && registrationId.Length > 0) {
          Android.Util.Log.Info(LogTag, "GCM registration successful. Registration Id: " + registrationId);
          ParseInstallation installation = ParseInstallation.CurrentInstallation;

          // Set `pushType` via internal `Set` method since we want to skip mutability check.
          installation.Set("pushType", "gcm");
          installation.DeviceToken = registrationId;

          return installation.SaveAsync();
        }
      }
      return Task.FromResult(0);
    }

    /// <summary>
    /// Encapsulates the GCM registration request-response, potentially using <c>AlarmManager</c> to
    /// schedule retries if the GCM service is not available.
    /// </summary>
    private class Request {
      private Context context;
      private string senderId;
      private PendingIntent appIntent;

      public static Request CreateAndSend(Context context, string senderId) {
        Request request = new Request(context, senderId);
        request.Send();

        return request;
      }

      private Request(Context context, string senderId) {
        this.context = context;
        this.senderId = senderId;
        appIntent = PendingIntent.GetBroadcast(context, 0, new Intent(), 0);
      }
        
      private void Send() {
        Intent intent = new Intent(IntentRegisterAction);
        intent.SetPackage("com.google.android.gsf");
        intent.PutExtra("sender", senderId);
        intent.PutExtra("app", appIntent);

        ComponentName name = null;
        try {
          name = context.StartService(intent);
        } catch (Exception) {
          // Do nothing.
        }
      }
    }
  }
}

