using RevenantWorkspaceSidekick.Core.Models;
using RevenantWorkspaceSidekick.Rules.Xaml;
using Xunit;

namespace RevenantWorkspaceSidekick.Tests.Rules;

public class XamlRulesTests
{
    // RWS-XAML-001

    [Fact]
    public void XamlReaderRule_Triggers_OnParse()
    {
        var rule = new XamlReaderRule();
        var ctx = Cs("var obj = XamlReader.Parse(userXaml);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-XAML-001", findings[0].RuleId);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void XamlReaderRule_Triggers_OnLoad()
    {
        var rule = new XamlReaderRule();
        var ctx = Cs("var obj = XamlReader.Load(stream);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-XAML-001", findings[0].RuleId);
    }

    [Fact]
    public void XamlReaderRule_DoesNotTrigger_OnOtherMethods()
    {
        var rule = new XamlReaderRule();
        var ctx = Cs("var obj = SomeReader.Parse(data);");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-XAML-002 (C# side)

    [Fact]
    public void ResourceDictionaryRule_Triggers_OnNonLiteralUriAssignment()
    {
        var rule = new ResourceDictionaryRule();
        var ctx = Cs("dict.Source = new Uri(userPath);");

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-XAML-002", findings[0].RuleId);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void ResourceDictionaryRule_DoesNotTrigger_OnLiteralUri()
    {
        var rule = new ResourceDictionaryRule();
        var ctx = Cs("dict.Source = new Uri(\"pack://application:,,,/Themes/Dark.xaml\");");

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-XAML-002 (XAML side)

    [Fact]
    public void ResourceDictionaryRule_Triggers_OnBindingSource()
    {
        var rule = new ResourceDictionaryRule();
        var ctx = Xaml("""
            <ResourceDictionary>
              <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="{Binding ThemePath}"/>
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-XAML-002", findings[0].RuleId);
    }

    [Fact]
    public void ResourceDictionaryRule_DoesNotTrigger_OnStaticSource()
    {
        var rule = new ResourceDictionaryRule();
        var ctx = Xaml("""
            <ResourceDictionary>
              <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/Themes/Light.xaml"/>
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
            """);

        Assert.Empty(rule.Analyze(ctx));
    }

    // RWS-XAML-003

    [Fact]
    public void XamlCodeElementRule_Triggers_OnXCode()
    {
        var rule = new XamlCodeElementRule();
        var ctx = Xaml("""
            <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <x:Code><![CDATA[void Foo() {}]]></x:Code>
            </Window>
            """);

        var findings = rule.Analyze(ctx).ToList();
        Assert.Single(findings);
        Assert.Equal("RWS-XAML-003", findings[0].RuleId);
        Assert.Equal(Severity.Medium, findings[0].Severity);
    }

    [Fact]
    public void XamlCodeElementRule_DoesNotTrigger_OnNormalXaml()
    {
        var rule = new XamlCodeElementRule();
        var ctx = Xaml("""
            <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <TextBlock Text="Hello"/>
            </Window>
            """);

        Assert.Empty(rule.Analyze(ctx));
    }

    private static FileContext Cs(string code) =>
        new("Test.cs", "Test.cs",
            "using System.Windows.Markup;\nusing System;\n" + code);

    private static FileContext Xaml(string content) =>
        new("Test.xaml", "Test.xaml", content);
}
