using System.Windows.Markup;

namespace CursedApp.Services;

// RWS-XAML-001: XamlReader.Parse with dynamic content
// RWS-XAML-002: ResourceDictionary.Source from non-literal URI
public class XamlService
{
    public object LoadTheme(string themeXaml)
    {
        // RWS-XAML-001: dynamic XAML parsing
        return XamlReader.Parse(themeXaml);
    }

    public void ApplyUserTheme(string themePath)
    {
        // RWS-XAML-002: ResourceDictionary.Source from non-literal
        var dict = new System.Windows.ResourceDictionary
        {
            Source = new Uri(themePath)
        };
        System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
    }
}
