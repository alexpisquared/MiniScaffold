﻿using Microsoft.Windows.ApplicationModel.Resources;

namespace QStatsTS4WinUI.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceLoader _resourceLoader = new();

    public static string GetLocalized(this string resourceKey) => _resourceLoader.GetString(resourceKey);
}
