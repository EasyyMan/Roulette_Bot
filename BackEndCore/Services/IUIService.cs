using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BackEndCore.Services;

public interface IUIService
{
    IUIOverlayService GetNewOverlay();

    void ShowErrorMessage(string text);
}
