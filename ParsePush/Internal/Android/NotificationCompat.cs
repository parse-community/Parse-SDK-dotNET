using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Java.Lang;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Parse.Internal {
  /// <summary>
  /// A simple implementation of the NotificationCompat class from Android.Support.V4.
  /// </summary>
  /// <remarks>
  /// It only differentiates between devices before and after JellyBean because the only extra feature
  /// that we currently support between the two device types is BigTextStyle notifications.
  /// This class takes advantage of lazy class loading to eliminate warnings of type
  /// 'Could not find class...'
  /// </remarks>
  internal class NotificationCompat {
#pragma warning disable 612, 618
    public const int PriorityDefault = 0;

    private static NotificationCompatImpl impl;
    private static NotificationCompatImpl Impl {
      get {
        if (impl == null) {
          if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean) {
            impl = new NotificationCompatPostJellyBean();
          } else {
            impl = new NotificationCompatImplBase();
          }
        }
        return impl;
      }
    }

    public interface NotificationCompatImpl {
      Notification Build(Builder b);
    }

    internal class NotificationCompatImplBase : NotificationCompatImpl {
      public Notification Build(Builder b) {
        Notification result = b.Notification;
        result.SetLatestEventInfo(b.Context, b.ContentTitle, b.ContentText, b.ContentIntent);

        return result;
      }
    }

    internal class NotificationCompatPostJellyBean : NotificationCompatImpl {
      public Notification Build(Builder b) {
        Notification.Builder builder = new Notification.Builder(b.Context);
        builder.SetContentTitle(b.ContentTitle)
          .SetContentText(b.ContentText)
          .SetTicker(b.Notification.TickerText)
          .SetSmallIcon(b.Notification.Icon, b.Notification.IconLevel)
          .SetContentIntent(b.ContentIntent)
          .SetDeleteIntent(b.Notification.DeleteIntent)
          .SetAutoCancel((b.Notification.Flags & NotificationFlags.AutoCancel) != 0)
          .SetLargeIcon(b.LargeIcon)
          .SetDefaults(b.Notification.Defaults);

        if (b.Style != null) {
          if (b.Style is BigTextStyle) {
            BigTextStyle staticStyle = b.Style as BigTextStyle;
            Notification.BigTextStyle style = new Notification.BigTextStyle(builder);
            style.SetBigContentTitle(staticStyle.BigContentTitle)
              .BigText(staticStyle.bigText);
            if (staticStyle.SummaryTextSet) {
              style.SetSummaryText(staticStyle.SummaryText);
            }
          }
        }

        return builder.Build();
      }
    }

    /// <summary>
    /// Builder class for <see cref="NotificationCompat"/> objects.
    /// </summary>
    /// <seealso href="http://developer.android.com/reference/android/support/v4/app/NotificationCompat.Builder.html"/>
    /// <remarks>
    /// Allows easier control over all the flags, as well as help constructing the typical notification layouts.
    /// </remarks>
    public class Builder {
      private const int MaxCharSequenceLength = 5 * 1024;

      public Context Context { get; private set; }
      public ICharSequence ContentTitle { get; private set; }
      public ICharSequence ContentText { get; private set; }
      public PendingIntent ContentIntent { get; private set; }
      public Bitmap LargeIcon { get; private set; }
      public int Priority { get; private set; }
      public Style Style { get; private set; }
      [Obsolete]
      public Notification Notification { get; private set; }

      public Builder(Context context) {
        Context = context;
        Notification = new Notification();

        Notification.When = Java.Lang.JavaSystem.CurrentTimeMillis();
        Notification.AudioStreamType = Stream.NotificationDefault;
        Priority = PriorityDefault;
      }

      public Builder SetWhen(long when) {
        Notification.When = when;
        return this;
      }

      public Builder SetSmallIcon(int icon) {
        Notification.Icon = icon;
        return this;
      }

      public Builder SetSmallIcon(int icon, int iconLevel) {
        Notification.Icon = icon;
        Notification.IconLevel = iconLevel;
        return this;
      }

      public Builder SetContentTitle(ICharSequence title) {
        ContentTitle = limitCharSequenceLength(title);
        return this;
      }

      public Builder SetContentText(ICharSequence text) {
        ContentText = limitCharSequenceLength(text);
        return this;
      }

      public Builder SetContentIntent(PendingIntent intent) {
        ContentIntent = intent;
        return this;
      }

      public Builder SetDeleteIntent(PendingIntent intent) {
        Notification.DeleteIntent = intent;
        return this;
      }

      public Builder SetTicker(ICharSequence tickerText) {
        Notification.TickerText = limitCharSequenceLength(tickerText);
        return this;
      }

      public Builder SetLargeIcon(Bitmap icon) {
        LargeIcon = icon;
        return this;
      }

      public Builder SetAutoCancel(bool autoCancel) {
        setFlag(NotificationFlags.AutoCancel, autoCancel);
        return this;
      }

      public Builder SetDefaults(NotificationDefaults defaults) {
        Notification.Defaults = defaults;
        if ((defaults & NotificationDefaults.Lights) != 0) {
          Notification.Flags |= NotificationFlags.ShowLights;
        }
        return this;
      }

      private void setFlag(NotificationFlags mask, bool value) {
        if (value) {
          Notification.Flags |= mask;
        } else {
          Notification.Flags &= ~mask;
        }
      }

      public Builder SetPriority(int pri) {
        Priority = pri;
        return this;
      }

      public Builder SetStyle(Style style) {
        if (Style != style) {
          Style = style;
          if (Style != null) {
            Style.SetBuilder(this);
          }
        }
        return this;
      }

      public Notification Build() {
        return Impl.Build(this);
      }

      private static ICharSequence limitCharSequenceLength(ICharSequence cs) {
        if (cs == null) {
          return cs;
        }
        if (cs.Length() > MaxCharSequenceLength) {
          cs = cs.SubSequenceFormatted(0, MaxCharSequenceLength);
        }
        return cs;
      }
    }

    /// <summary>
    /// An object that can apply a rich notification style to a <see cref="NotificationCompat.Builder"/> object.
    /// </summary>
    public abstract class Style {
      protected Builder builder;
      public ICharSequence BigContentTitle { get; protected set; }
      public ICharSequence SummaryText { get; protected set; }

      private bool summaryTextSet = false;
      public bool SummaryTextSet {
        get { return summaryTextSet; }
        protected set { summaryTextSet = value; }
      }

      public void SetBuilder(Builder builder) {
        if (this.builder != builder) {
          this.builder = builder;
          if (this.builder != null) {
            this.builder.SetStyle(this);
          }
        }
      }

      public Notification Build() {
        Notification notification = null;
        if (builder != null) {
          notification = builder.Build();
        }
        return notification;
      }
    }

    public class BigTextStyle : Style {
      internal ICharSequence bigText;

      public BigTextStyle() {
      }

      public BigTextStyle(Builder builder) {
        SetBuilder(builder);
      }

      /// <summary>
      /// Overrides <see cref="Builder.ContentTitle"/> in the big form of the template.
      /// </summary>
      /// <remarks>
      /// This defaults to the value passed to SetContentTitle().
      /// </remarks>
      /// <param name="title"></param>
      /// <returns></returns>
      public BigTextStyle SetBigContentTitle(ICharSequence title) {
        BigContentTitle = title;
        return this;
      }

      /// <summary>
      /// Set the first line of text after the detail section in the big form of the template.
      /// </summary>
      /// <param name="summaryText"></param>
      /// <returns></returns>
      public BigTextStyle SetSummaryText(ICharSequence summaryText) {
        SummaryText = summaryText;
        SummaryTextSet = true;
        return this;
      }

      /// <summary>
      /// Provide the longer text to be displayed in the big form of the
      /// template in place of the content text.
      /// </summary>
      /// <param name="bigText"></param>
      /// <returns></returns>
      public BigTextStyle BigText(ICharSequence bigText) {
        this.bigText = bigText;
        return this;
      }
    }
  }
#pragma warning restore 612, 618
}