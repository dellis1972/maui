using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Maui.Controls
{
	public partial class Layout
	{
		public static void MapInputTransparent(LayoutHandler handler, Layout layout)
		{
			// Handle input transparent for this view
			// TODO ezhart LayoutHandler.Android.cs is doing the same thing, make it one reusable method
			if (handler.PlatformView is LayoutViewGroup layoutViewGroup)
			{
				layoutViewGroup.InputTransparent = layout.InputTransparent;
			}

			layout.UpdateDescendantInputTransparent();


			// Once we've got this working in general, we need to investigate what it looks like for performance on Android
			// Because a layout with 100 controls in it going transparent with CIT on is going to wrap every single child control
			// Can we optimize that by setting the right kind of wrapping on the layout? or some other property? can the invididual 
			// controls skip wrapping if they find a wrapped layout higher up the tree that has CIT turned on?
		}
	}
}
