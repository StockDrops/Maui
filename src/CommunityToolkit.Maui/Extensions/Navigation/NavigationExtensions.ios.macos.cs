﻿using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UI.Views;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Extensions;

public static partial class NavigationExtensions
{
	static void PlatformShowPopup(BasePopup popup, IMauiContext mauiContext)
	{
		var popupNative = popup.ToHandlerInternal(mauiContext);
		popupNative.Invoke(nameof(IBasePopup.OnOpened));
	}

	static Task<T?> PlatformShowPopupAsync<T>(Popup<T> popup, IMauiContext mauiContext)
	{
		PlatformShowPopup(popup, mauiContext);

		return popup.Result;
	}
}

static class ExtensionsToDelete
{
	public static INativeViewHandler ToHandlerInternal(this IElement view, IMauiContext context)
	{
		_ = view ?? throw new ArgumentNullException(nameof(view));
		_ = context ?? throw new ArgumentNullException(nameof(context));

		//This is how MVU works. It collapses views down
		if (view is IReplaceableView ir)
			view = ir.ReplacedView;

		var handler = view.Handler;
		if (handler == null)
			handler = context.Handlers.GetHandler(view.GetType());

		if (handler == null)
			throw new Exception($"Handler not found for view {view}.");

		handler.SetMauiContext(context);

		view.Handler = handler;

		if (handler.VirtualView != view)
			handler.SetVirtualView(view);

		return (INativeViewHandler)handler;
	}
}
