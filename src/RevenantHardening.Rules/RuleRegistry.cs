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
using RevenantHardening.Rules.Cpp;
using RevenantHardening.Rules.Dart;
using RevenantHardening.Rules.Go;
using RevenantHardening.Rules.Kotlin;
using RevenantHardening.Rules.Perl;
using RevenantHardening.Rules.Php;
using RevenantHardening.Rules.Rust;
using RevenantHardening.Rules.Swift;
using RevenantHardening.Rules.Web;

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
        new PythonHardcodedAbsolutePathRule(),
        new PythonShadowingBuiltinRule(),
        new PythonGlobalKeywordRule(),
        new PythonWildcardImportRule(),
        new PythonMissingMainBlockRule(),
        new PythonDeepNestingRule(),
        new PythonLargeFunctionRule(),
        new PythonBadNamingRule(),

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

        // RSH-CPP-*
        new CppUnsafeStringFunctionRule(),
        new CppMemoryLeakRule(),
        new CppCStyleCastRule(),
        new CppCommandInjectionRule(),
        new CppUsingNamespaceStdRule(),

        // RSH-DT-*
        new DartDynamicCodeRule(),
        new DartHardcodedSecretRule(),
        new DartSilentFailureRule(),
        new DartPrintStatementRule(),

        // RSH-GO-*
        new GoCommandInjectionRule(),
        new GoHardcodedSecretRule(),
        new GoSqlInjectionRule(),

        // RSH-KT-*
        new KotlinHardcodedSecretRule(),
        new KotlinSilentFailureRule(),
        new KotlinForceUnwrapRule(),
        new KotlinSqlInjectionRule(),

        // RSH-PL-*
        new PerlCommandInjectionRule(),
        new PerlHardcodedSecretRule(),

        // RSH-PHP-*
        new PhpCommandInjectionRule(),
        new PhpSqlInjectionRule(),

        // RSH-RS-*
        new RustUnsafeBlockRule(),
        new RustUnwrapRule(),

        // RSH-SW-*
        new SwiftForceUnwrapRule(),
        new SwiftSilentFailureRule(),

        // RSH-WEB-*
        new WebMixedContentRule(),

        // RSH-COM-*
        new TodoFixmeRule(),
    ];
}
