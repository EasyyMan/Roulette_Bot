using BackEndCore.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BackEndCore.Services;

public interface IUIOverlayService
{
    ObservableCollection<TargetBox> GetTargetBoxes();

    bool IsLocked();

    void ShowOverlayWindow();

    void CloseOverlayWindow();

    void LoadBoxesConfiguration();
}
