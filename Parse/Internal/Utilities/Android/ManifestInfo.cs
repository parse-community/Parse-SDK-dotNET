using System;
using System.IO;
using System.Collections.Generic;

using Android.OS;
using Android.App;
using Android.Content;
using Android.Content.PM;

using Java.Lang;

namespace Parse {
  /// <summary>
  /// A utility class for retrieving app metadata such as the app name, default icon, whether or not
  /// the app declares the correct permissions for push, etc.
  /// </summary>
  // TODO (hallucinogen): make it instance based
  internal class ManifestInfo {
    private const string LogTag = "parse.ManifestInfo";

    /// <summary>
    /// ParsePushBroadcastReceiver intents: ACTION_PUSH_RECEIVE, ACTION_PUSH_OPEN, ACTION_PUSH_DELETE
    /// </summary>
    private const int numberOfPushIntents = 3;

    private static readonly object mutex = new object();

    #region Missing Permissions Message

    private const string MissingParsePushServiceMessage = "<service android:name=\"parse.ParsePushService\" />.\n";

    private static string MissingParsePushBroadcastReceiverMessage {
      get {
        string gcmPackagePermission = PackageName + ".permission.C2D_MESSAGE";
        return
          "<uses-permission android:name=\"android.permission.INTERNET\" />\n" +
          "<uses-permission android:name=\"android.permission.ACCESS_NETWORK_STATE\" />\n" +
          "<uses-permission android:name=\"android.permission.VIBRATE\" />\n" +
          "<uses-permission android:name=\"android.permission.WAKE_LOCK\" />\n" +
          "<uses-permission android:name=\"android.permission.GET_ACCOUNTS\" />\n" +
          "<uses-permission android:name=\"com.google.android.c2dm.permission.RECEIVE\" />\n" +
          "<permission android:name=\"" + gcmPackagePermission + "\" " +
          "android:protectionLevel=\"signature\" />\n" +
          "<uses-permission android:name=\"" + gcmPackagePermission + "\" />\n";
      }
    }

    private static string MissingApplicationPermissionMessage {
      get {
        return
          "<receiver android:name=\"parse.ParsePushBroadcastReceiver\" />.\n" +
          "android:permission=\"com.google.android.c2dm.permission.SEND\">\n" +
          "  <intent-filter>\n" +
          "    <action android:name=\"com.google.android.c2dm.intent.RECEIVE\" />\n" +
          "    <action android:name=\"com.google.android.c2dm.intent.REGISTRATION\" />\n" +
          "    <category android:name=\"" + PackageName + "\" />\n" +
          "  </intent-filter>\n" +
          "</receiver>\n";
      }
    }

    #endregion

    private static long lastModified = -1;
    /// <summary>
    /// Returns the last time this application's APK was modified on disk. This is a proxy for both
    /// version changes and if the APK has been restored from backup onto a different device.
    /// </summary>
    public static long LastModified {
      get {
        lock (mutex) {
          if (lastModified == -1) {
            lastModified = File.GetLastWriteTime(ApplicationInfo.SourceDir).Millisecond;
          }
        }
        return lastModified;
      }
    }

    private static string displayName;
    /// <summary>
    /// Returns the display name of the app used by the app launcher, as specified by the <c>android:label</c>
    /// attribute in <c>&lt;application&gt;</c> element of the manifest.
    /// </summary>
    public static string DisplayName {
      get {
        lock (mutex) {
          if (displayName == null) {
            displayName = PackageManager.GetApplicationLabel(ApplicationInfo);
          }
        }
        return displayName;
      }
    }

    private static int iconId = -1;
    /// <summary>
    /// Returns the default icon id used by this application as specified by the <c>android:icon</c> attribute
    /// in <c>&lt;application&gt;</c> element of the manifest.
    /// </summary>
    public static int IconId {
      get {
        lock (mutex) {
          if (iconId == -1) {
            iconId = ApplicationInfo.Icon;
          }
        }
        return iconId;
      }
    }

    private static int pushIconId = -1;
    public static int PushIconId {
      get {
        lock (mutex) {
          if (pushIconId == -1) {
            var customIcon = GetApplicationMetaData().GetInt("com.parse.push.notification_icon", -1);
            pushIconId = customIcon == -1 ? IconId : customIcon;
          }
          return pushIconId;
        }
      }
    }

    private static Intent launcherIntent;
    public static Intent LauncherIntent {
      get {
        lock (mutex) {
          launcherIntent = PackageManager.GetLaunchIntentForPackage(PackageName);
        }
        return launcherIntent;
      }
    }

    private static string versionName;
    /// <summary>
    /// Returns the version name for this app, as specified by the <c>android:versionName</c> attribute in the
    /// <c>&lt;manifest&gt;</c> element of the manifest.
    /// </summary>
    public static string VersionName {
      get {
        lock (mutex) {
          if (versionName == null) {
            versionName = getPackageInfo(PackageName).VersionName;
          }
        }
        return versionName;
      }
    }

    private static int versionCode = -1;
    /// <summary>
    /// Returns the version name for this app, as specified by the <c>android:versionName</c> attribute in the
    /// <c>&lt;manifest&gt;</c> element of the manifest.
    /// </summary>
    public static int VersionCode {
      get {
        lock (mutex) {
          if (versionCode == -1) {
            versionCode = getPackageInfo(PackageName).VersionCode;
          }
        }
        return versionCode;
      }
    }

    public static Bundle GetApplicationMetaData() {
      try {
        ApplicationInfo info = PackageManager.GetApplicationInfo(PackageName, PackageInfoFlags.MetaData);
        if (info != null) {
          return info.MetaData;
        }
      } catch {
        // Do nothing.
      }
      return null;
    }

    /// <summary>
    /// Returns the package name of this app.
    /// </summary>
    public static string PackageName {
      get {
        return ManifestInfo.Context.PackageName;
      }
    }

    /// <summary>
    /// Returns true if current device supports GCM.
    /// </summary>
    public static bool DeviceSupportGCM {
      get {
        return ((int)Build.VERSION.SdkInt >= 8 && ManifestInfo.getPackageInfo("com.google.android.gsf") != null);
      }
    }

    /// <summary>
    /// Throws exception if the <c>AndroidManifest.xml</c> file doesn't contain any the required Service, BroadcastReceiver and permissions
    /// to receive GCM push notifications.
    /// </summary>
    public static bool HasPermissionForGCM() {
      bool hasParsePushService = getServiceInfo(typeof(ParsePushService)) != null;
      bool hasApplicationPermission = hasPermissions(
          "android.permission.INTERNET",
          "android.permission.ACCESS_NETWORK_STATE",
          "android.permission.WAKE_LOCK",
          "android.permission.GET_ACCOUNTS",
          "com.google.android.c2dm.permission.RECEIVE",
          PackageName + ".permission.C2D_MESSAGE"
        );
      Intent[] intents = new Intent[] {
        new Intent("com.google.android.c2dm.intent.RECEIVE")
          .SetPackage(PackageName)
          .AddCategory(PackageName)
      };
      string receiverPermission = "com.google.android.c2dm.permission.SEND";
      bool hasBroadcastReceiverPermission = ManifestInfo.hasReceiverPermission(typeof(ParsePushBroadcastReceiver), receiverPermission, intents);

      if (hasParsePushService && hasApplicationPermission && hasBroadcastReceiverPermission) {
        return true;
      }

      // Print errors.
      Android.Util.Log.Warn(LogTag, "Cannot use GCM for push because AndroidManifest.xml is missing:\n"
        + (hasParsePushService ? "" : MissingParsePushServiceMessage)
        + (hasApplicationPermission ? "" : MissingApplicationPermissionMessage)
        + (hasBroadcastReceiverPermission ? "" : MissingParsePushBroadcastReceiverMessage));

      return false;
    }

    private static PackageManager PackageManager {
      get {
        return ManifestInfo.Context.PackageManager;
      }
    }

    private static Context Context {
      get {
        return Application.Context;
      }
    }

    private static ApplicationInfo ApplicationInfo {
      get {
        return ManifestInfo.Context.ApplicationInfo;
      }
    }

    private static bool hasPermissions(params string[] permissions) {
      foreach (string permission in permissions) {
        if (PackageManager.CheckPermission(permission, PackageName) != Permission.Granted) {
          return false;
        }
      }
      return true;
    }

    private static ServiceInfo getServiceInfo(Type serviceType) {
      ServiceInfo info = null;

      try {
        info = PackageManager.GetServiceInfo(new ComponentName(ManifestInfo.Context, Class.FromType(serviceType)), 0);
      } catch {
        // Do nothing.
      }
      return info;
    }

    private static ActivityInfo getReceiverInfo(Type receiverType) {
      ActivityInfo info = null;

      try {
        info = PackageManager.GetReceiverInfo(new ComponentName(ManifestInfo.Context, Class.FromType(receiverType)), 0);
      } catch {
        // Do nothing.
      }
      return info;
    }

    private static bool hasReceiverPermission(Type receiverType, string permission, Intent[] intents) {
      ActivityInfo receiver = getReceiverInfo(receiverType);

      if (receiver == null) {
        return false;
      }

      if (permission != null && !permission.Equals(receiver.Permission)) {
        return false;
      }

      foreach (var intent in intents) {
        IList<ResolveInfo> resolveInfo = PackageManager.QueryBroadcastReceivers(intent, 0);
        if (resolveInfo.Count == 0) {
          return false;
        }

        if (!hasReceiverInfo(receiverType, resolveInfo)) {
          return false;
        }
      }

      return true;
    }

    private static bool hasReceiverInfo(Type receiverType, IList<ResolveInfo> resolveInfo) {
      var typeJavaName = Class.FromType(receiverType).CanonicalName;
      foreach (var info in resolveInfo) {
        if (info.ActivityInfo != null && typeJavaName == info.ActivityInfo.Name) {
          return true;
        }
      }
      return false;
    }

    private static PackageInfo getPackageInfo(string name) {
      PackageInfo info = null;

      try {
        info = PackageManager.GetPackageInfo(name, 0);
      } catch {
        // Do nothing.
      }
      return info;
    }
  }
}

