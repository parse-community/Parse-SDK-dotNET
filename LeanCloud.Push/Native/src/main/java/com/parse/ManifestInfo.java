/*
 * Copyright (c) 2015-present, Parse, LLC.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */
package com.parse;

import java.io.File;
import java.util.ArrayList;
import java.util.List;

import android.app.Service;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.pm.ActivityInfo;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.pm.PackageManager.NameNotFoundException;
import android.content.pm.ResolveInfo;
import android.content.pm.ServiceInfo;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;

/**
 * A utility class for retrieving app metadata such as the app name, default icon, whether or not
 * the app declares the correct permissions for push, etc.
 */
/** package */ class ManifestInfo {
  private static final String TAG = "com.parse.ManifestInfo";

  private final Object lock = new Object();
  private long lastModified = -1;
  private int versionCode = -1;
  private String versionName;
  private int iconId = 0;
  private int pushIconId = 0;
  private String displayName;
  private String packageName;
  private PushType pushType;
  private Context context;
  private Intent launcherIntent;

  /* package */ ManifestInfo(Context context) {
    this.context = context;
    ApplicationInfo appInfo = context.getApplicationInfo();
    PackageManager packageManager = context.getPackageManager();
    packageName = context.getPackageName();
    launcherIntent = packageManager.getLaunchIntentForPackage(packageName);

    try {
      PackageInfo packageInfo = packageManager.getPackageInfo(packageName, 0);

      versionCode = packageInfo.versionCode;
      versionName = packageInfo.versionName;
    } catch (NameNotFoundException ex) {
      // Do nothing
    }

    File apkPath = new File(appInfo.sourceDir);
    lastModified = apkPath.lastModified();

    displayName = packageManager.getApplicationLabel(appInfo).toString();
    iconId = appInfo.icon;
    pushIconId = getApplicationMetadata().getInt("com.parse.push.notification_icon", 0);
    if (pushIconId == 0) {
      pushIconId = iconId;
    }
  }

  /**
   * Returns the last time this application's APK was modified on disk. This is a proxy for both
   * version changes and if the APK has been restored from backup onto a different device.
   */
  public long getLastModified() {
    return lastModified;
  }

  /**
   * Returns the version code for this app, as specified by the android:versionCode attribute in the
   * <manifest> element of the manifest.
   */
  public int getVersionCode() {
    return versionCode;
  }

  /**
   * Returns the version name for this app, as specified by the android:versionName attribute in the
   * <manifest> element of the manifest.
   */
  public String getVersionName() {
    return versionName;
  }

  /**
   * Returns the display name of the app used by the app launcher, as specified by the android:label
   * attribute in the <application> element of the manifest.
   */
  public String getDisplayName() {
    return displayName;
  }

  /**
   * Returns the package name of the app, as specified by the package attribute in the
   * <manifest> element of the manifest.
   */
  public String getPackageName() {
    return packageName;
  }

  /**
   * Returns the intent that will launch main activity, as specified by the <activity> element with
   * android.intent.action.MAIN <intent-filter> in <application> element of the manifest.
   */
  public Intent getLauncherIntent() {
    return launcherIntent;
  }

  /**
   * Returns the default icon id used by this application, as specified by the android:icon
   * attribute in the <application> element of the manifest.
   */
  public int getIconId() {
    return iconId;
  }

  /**
   * Returns the default icon id used by this application for push notification, as specified by
   * <meta-data android:name="com.parse.push.notification_icon"> of the manifest. Default to iconId
   * if none specified.
   */
  public int getPushIconId() {
    return pushIconId;
  }

  /**
   * Returns a list of ResolveInfo objects corresponding to the BroadcastReceivers with Intent
   * Filters specifying the given action within the app's package.
   */
  /* package */ List<ResolveInfo> getIntentReceivers(String... actions) {
    List<ResolveInfo> list = new ArrayList<>();

    for (String action : actions) {
      list.addAll(
          context.getPackageManager().queryBroadcastReceivers(
              new Intent(action),
              PackageManager.GET_INTENT_FILTERS));
    }

    for (int i = list.size() - 1; i >= 0; --i) {
      String receiverPackageName = list.get(i).activityInfo.packageName;
      if (!receiverPackageName.equals(packageName)) {
        list.remove(i);
      }
    }
    return list;
  }

  /**
   * Inspects the app's manifest and returns whether the manifest contains required declarations to
   * be able to use GCM.
   */
  public PushType getPushType() {
    synchronized (lock) {
      if (pushType == null) {
        boolean deviceSupportsGcm = deviceSupportsGcm();
        boolean hasAnyGcmSpecificDeclaration = hasAnyGcmSpecificDeclaration();
        ManifestCheckResult gcmSupportLevel = gcmSupportLevel();

        if (deviceSupportsGcm &&
            gcmSupportLevel != ManifestCheckResult.MISSING_REQUIRED_DECLARATIONS) {
          pushType = PushType.GCM;
        } else {
          pushType = PushType.NONE;
        }

        /*
         * If we selected gcm/ppns but the manifest is missing some optional declarations, warn so
         * the user knows how to add those optional declarations.
         */
        if (pushType == PushType.GCM &&
            gcmSupportLevel == ManifestCheckResult.MISSING_OPTIONAL_DECLARATIONS) {
          Log.w(TAG, "Using GCM for push, but the app manifest is missing some optional " +
              "declarations that should be added for maximum reliability. Please " +
              getGcmManifestMessage());
        }

        /*
         * If the user added some GCM-specific declarations, but not all of the required ones, then
         * this is a sign that the user wants to use GCM but has a misconfigured manifest. Log an
         * error (not just a warning) in this case.
         */
        if (pushType == PushType.NONE && hasAnyGcmSpecificDeclaration) {
          if (gcmSupportLevel != ManifestCheckResult.HAS_ALL_DECLARATIONS) {
            Log.e(TAG, "Cannot use GCM for push because the app manifest is missing some " +
                "required declarations. Please " + getGcmManifestMessage());
          }
        }
      }
    }

    return pushType;
  }

  /* package */ enum ManifestCheckResult {
    /*
     * Manifest has all required and optional declarations necessary to support this push service.
     */
    HAS_ALL_DECLARATIONS,

    /*
     * Manifest has all required declarations to support this push service, but is missing some
     * optional declarations.
     */
    MISSING_OPTIONAL_DECLARATIONS,

    /*
     * Manifest doesn't have enough required declarations to support this push service.
     */
    MISSING_REQUIRED_DECLARATIONS
  }

  private Context getContext() {
    return context;
  }

  /**
   * @return A {@link Bundle} if meta-data is specified in AndroidManifest, otherwise null.
   */
  public Bundle getApplicationMetadata() {
    try {
      ApplicationInfo info = context.getPackageManager()
          .getApplicationInfo(packageName, PackageManager.GET_META_DATA);
      if (info != null) {
        return info.metaData;
      }
    } catch (NameNotFoundException ex) {
      // Do nothing.
    }
    return null;
  }

  private PackageInfo getPackageInfo(String name) {
    PackageInfo info = null;

    try {
      info = context.getPackageManager().getPackageInfo(name, 0);
    } catch (NameNotFoundException e) {
      // do nothing
    }

    return info;
  }

  private ServiceInfo getServiceInfo(Class<? extends Service> clazz) {
    ServiceInfo info = null;

    try {
      info = context.getPackageManager().getServiceInfo(new ComponentName(context, clazz), 0);
    } catch (NameNotFoundException e) {
      // do nothing
    }

    return info;
  }

  private ActivityInfo getReceiverInfo(Class<? extends BroadcastReceiver> clazz) {
    ActivityInfo info = null;

    try {
      info = context.getPackageManager().getReceiverInfo(new ComponentName(context, clazz), 0);
    } catch (NameNotFoundException e) {
      // do nothing
    }

    return info;
  }

  private boolean hasPermissions(String... permissions) {
    for (String permission : permissions) {
      if (context.getPackageManager()
          .checkPermission(permission, packageName) != PackageManager.PERMISSION_GRANTED) {
        return false;
      }
    }

    return true;
  }

  private static boolean checkResolveInfo(Class<? extends BroadcastReceiver> clazz,
      List<ResolveInfo> infoList) {
    for (ResolveInfo info : infoList) {
      if (info.activityInfo != null && clazz.getCanonicalName().equals(info.activityInfo.name)) {
        return true;
      }
    }

    return false;
  }

  private boolean checkReceiver(Class<? extends BroadcastReceiver> clazz,
      String permission, Intent[] intents) {
    ActivityInfo receiver = getReceiverInfo(clazz);

    if (receiver == null) {
      return false;
    }

    if (permission != null && !permission.equals(receiver.permission)) {
      return false;
    }

    for (Intent intent : intents) {
      List<ResolveInfo> receivers = context.getPackageManager().queryBroadcastReceivers(intent, 0);
      if (receivers.isEmpty()) {
        return false;
      }

      if (!checkResolveInfo(clazz, receivers)) {
        return false;
      }
    }

    return true;
  }

  public boolean hasPermissionForGcm() {
    return getPushType() == PushType.GCM;
  }

  private boolean hasAnyGcmSpecificDeclaration() {
    if (hasPermissions("com.google.android.c2dm.permission.RECEIVE") ||
        hasPermissions(packageName + ".permission.C2D_MESSAGE") ||
        getReceiverInfo(ParsePushBroadcastReceiver.class) != null) {
      return true;
    }

    return false;
  }

  private boolean deviceSupportsGcm() {
    return Build.VERSION.SDK_INT >= 8 && getPackageInfo("com.google.android.gsf") != null;
  }

  private ManifestCheckResult gcmSupportLevel() {
    if (getServiceInfo(ParsePushService.class) == null) {
      return ManifestCheckResult.MISSING_REQUIRED_DECLARATIONS;
    }

    String[] requiredPermissions = new String[] {
        "android.permission.INTERNET",
        "android.permission.ACCESS_NETWORK_STATE",
        "android.permission.WAKE_LOCK",
        "android.permission.GET_ACCOUNTS",
        "com.google.android.c2dm.permission.RECEIVE",
        packageName + ".permission.C2D_MESSAGE"
    };

    if (!hasPermissions(requiredPermissions)) {
      return ManifestCheckResult.MISSING_REQUIRED_DECLARATIONS;
    }

    String rcvrPermission = "com.google.android.c2dm.permission.SEND";
    Intent[] intents = new Intent[] {
        new Intent(ParsePushService.ACTION_GCM_RECEIVE)
            .setPackage(packageName)
            .addCategory(packageName),
    };

    if (!checkReceiver(ParsePushBroadcastReceiver.class, rcvrPermission, intents)) {
      return ManifestCheckResult.MISSING_REQUIRED_DECLARATIONS;
    }

    String[] optionalPermissions = new String[] {
        "android.permission.VIBRATE"
    };

    if (!hasPermissions(optionalPermissions)) {
      return ManifestCheckResult.MISSING_OPTIONAL_DECLARATIONS;
    }

    return ManifestCheckResult.HAS_ALL_DECLARATIONS;
  }

  private String getGcmManifestMessage() {
    String gcmPackagePermission = packageName + ".permission.C2D_MESSAGE";
    return "make sure that these permissions are declared as children " +
        "of the root <manifest> element:\n" +
        "\n" +
        "<uses-permission android:name=\"android.permission.INTERNET\" />\n" +
        "<uses-permission android:name=\"android.permission.ACCESS_NETWORK_STATE\" />\n" +
        "<uses-permission android:name=\"android.permission.VIBRATE\" />\n" +
        "<uses-permission android:name=\"android.permission.WAKE_LOCK\" />\n" +
        "<uses-permission android:name=\"android.permission.GET_ACCOUNTS\" />\n" +
        "<uses-permission android:name=\"com.google.android.c2dm.permission.RECEIVE\" />\n" +
        "<permission android:name=\"" + gcmPackagePermission + "\" " +
        "android:protectionLevel=\"signature\" />\n" +
        "<uses-permission android:name=\"" + gcmPackagePermission + "\" />\n" +
        "\n" +
        "Also, please make sure that these services and broadcast receivers are declared as " +
        "children of the <application> element:\n" +
        "\n" +
        "<service android:name=\"com.parse.ParsePushService\" />\n" +
        "<receiver android:name=\"com.parse.ParsePushBroadcastReceiver\" " +
        "android:permission=\"com.google.android.c2dm.permission.SEND\">\n" +
        "  <intent-filter>\n" +
        "    <action android:name=\"com.google.android.c2dm.intent.RECEIVE\" />\n" +
        "    <action android:name=\"com.google.android.c2dm.intent.REGISTRATION\" />\n" +
        "    <category android:name=\"" + packageName + "\" />\n" +
        "  </intent-filter>\n" +
        "</receiver>\n";
  }
}
