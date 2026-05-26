using RevenantHardening.Core;
using RevenantHardening.Rules.Acl;
using RevenantHardening.Rules.Binary;
using RevenantHardening.Rules.Execution;
using RevenantHardening.Rules.Java;
using RevenantHardening.Rules.Msix;
using RevenantHardening.Rules.PInvoke;
using RevenantHardening.Rules.Python;
using RevenantHardening.Rules.Registry;
using RevenantHardening.Rules.Secrets;
using RevenantHardening.Rules.TypeScript;
using RevenantHardening.Rules.Xaml;
using RevenantHardening.Rules.Common;

namespace RevenantHardening.Rules;

public static class RuleRegistry
{
    public static readonly IReadOnlyList<IRule> All =
    [
        // RSH-MSIX-*
        new MsixCapabilityRule(),
        new RunFullTrustRule(),
        new DebugSigningRule(),
        new UapProtocolRule(),

        // RSH-REG-*
        new HklmWriteRule(),
        new WritableHandleRule(),
        new ElevationGuardRule(),
        new SetAccessControlRule(),

        // RSH-EXEC-*
        new ProcessStartRule(),
        new UseShellExecuteRule(),
        new AssemblyLoadRule(),
        new UriHandlerRule(),
        new ProcessStartInterpolationRule(),

        // RSH-SEC-*
        new ApiKeyPatternRule(),
        new ResourceSecretRule(),
        new ProjectMetadataSecretRule(),
        new ConnectionStringInCodeRule(),

        // RSH-XAML-*
        new XamlReaderRule(),
        new ResourceDictionaryRule(),
        new XamlCodeElementRule(),

        // RSH-PINVOKE-*
        new DllImportCharSetRule(),
        new DllImportDllNameRule(),
        new DangerousApiRule(),

        // RSH-ACL-*
        new FileAclRule(),
        new OpenAccessRuleRule(),

        // RSH-BIN-*
        new BinarySecretRule(),
        new BinaryConnectionStringRule(),

        // RSH-PY-*
        new DangerousEvalRule(),
        new SubprocessShellRule(),
        new InsecureDeserializationRule(),
        new WeakCryptographyRule(),
        new InsecureWebConfigRule(),
        new PythonHardcodedSecretRule(),
        new PythonSqlInjectionRule(),
        new PythonSilentFailureRule(),
        new PythonMissingTimeoutRule(),
        new PythonUnboundedLoopRule(),

        // RSH-JV-*
        new JavaCommandExecutionRule(),
        new JavaInsecureDeserializationRule(),
        new JavaWeakCryptoRule(),
        new JavaPathTraversalRule(),
        new JavaXmlXxeRule(),
        new JavaHardcodedSecretRule(),
        new JavaSqlInjectionRule(),
        new JavaSilentFailureRule(),
        new JavaUnboundedLoopRule(),

        // RSH-TS-*
        new TsDangerousEvalRule(),
        new TsCommandExecutionRule(),
        new TsInsecureCryptoRule(),
        new TsPrototypePollutionRule(),
        new TsReactHtmlInjectionRule(),
        new TypeScriptHardcodedSecretRule(),
        new TypeScriptSqlInjectionRule(),
        new TypeScriptSilentFailureRule(),
        new TypeScriptUnboundedLoopRule(),
        new TypeScriptMissingTimeoutRule(),

        // RSH-COM-*
        new TodoFixmeRule(),
    ];
}
