using BackEndCore.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Roulette_Bot.Helpers;

public class WpfUIService : IUIService
{
    public IUIOverlayService GetNewOverlay()
    {        
        //var overlayForm = App.AppHost!.Services.GetRequiredService<IUIOverlayService>();
        //ArgumentNullException.ThrowIfNull(overlayForm);

        OverlayWindow? overlay = null;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            overlay = new OverlayWindow();                   
        });

        ArgumentNullException.ThrowIfNull(overlay);
        return overlay;
    }

    public void ShowErrorMessage(string text)
    {
        MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
