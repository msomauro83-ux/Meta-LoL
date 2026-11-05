package crc6452ffdc5b34af3a0f;


public class MauiSwipeView_SwipeItemAccessibilityDelegate
	extends crc6452ffdc5b34af3a0f.AccessibilityDelegateCompatWrapper
	implements
		mono.android.IGCUserPeer
{

	public MauiSwipeView_SwipeItemAccessibilityDelegate ()
	{
		super ();
		if (getClass () == MauiSwipeView_SwipeItemAccessibilityDelegate.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.MauiSwipeView+SwipeItemAccessibilityDelegate, Microsoft.Maui", "", this, new java.lang.Object[] {  });
		}
	}

	public MauiSwipeView_SwipeItemAccessibilityDelegate (android.view.View.AccessibilityDelegate p0)
	{
		super (p0);
		if (getClass () == MauiSwipeView_SwipeItemAccessibilityDelegate.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.MauiSwipeView+SwipeItemAccessibilityDelegate, Microsoft.Maui", "Android.Views.View+AccessibilityDelegate, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public MauiSwipeView_SwipeItemAccessibilityDelegate (androidx.core.view.AccessibilityDelegateCompat p0)
	{
		super ();
		if (getClass () == MauiSwipeView_SwipeItemAccessibilityDelegate.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.MauiSwipeView+SwipeItemAccessibilityDelegate, Microsoft.Maui", "AndroidX.Core.View.AccessibilityDelegateCompat, Xamarin.AndroidX.Core", this, new java.lang.Object[] { p0 });
		}
	}

	public void onInitializeAccessibilityNodeInfo (android.view.View p0, androidx.core.view.accessibility.AccessibilityNodeInfoCompat p1)
	{
		n_onInitializeAccessibilityNodeInfo (p0, p1);
	}

	private native void n_onInitializeAccessibilityNodeInfo (android.view.View p0, androidx.core.view.accessibility.AccessibilityNodeInfoCompat p1);

	public boolean performAccessibilityAction (android.view.View p0, int p1, android.os.Bundle p2)
	{
		return n_performAccessibilityAction (p0, p1, p2);
	}

	private native boolean n_performAccessibilityAction (android.view.View p0, int p1, android.os.Bundle p2);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
