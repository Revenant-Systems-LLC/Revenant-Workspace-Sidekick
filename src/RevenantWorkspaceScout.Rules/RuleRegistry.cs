using RevenantWorkspaceScout.Core;
using RevenantWorkspaceScout.Rules.Acl;
using RevenantWorkspaceScout.Rules.Binary;
using RevenantWorkspaceScout.Rules.Execution;
using RevenantWorkspaceScout.Rules.Java;
using RevenantWorkspaceScout.Rules.Msix;
using RevenantWorkspaceScout.Rules.PInvoke;
using RevenantWorkspaceScout.Rules.Python;
using RevenantWorkspaceScout.Rules.Registry;
using RevenantWorkspaceScout.Rules.Secrets;
using RevenantWorkspaceScout.Rules.TypeScript;
using RevenantWorkspaceScout.Rules.Xaml;
using RevenantWorkspaceScout.Rules.Common;
using RevenantWorkspaceScout.Rules.Cpp;
using RevenantWorkspaceScout.Rules.Dart;
using RevenantWorkspaceScout.Rules.Go;
using RevenantWorkspaceScout.Rules.Kotlin;
using RevenantWorkspaceScout.Rules.Perl;
using RevenantWorkspaceScout.Rules.Php;
using RevenantWorkspaceScout.Rules.Rust;
using RevenantWorkspaceScout.Rules.Swift;
using RevenantWorkspaceScout.Rules.Web;

namespace RevenantWorkspaceScout.Rules;

public static class RuleRegistry
{
    public static readonly IReadOnlyList<IRule> All =
    [
        // RWS-MSIX-*
        new MsixCapabilityRule(),
        new RunFullTrustRule(),
        new DebugSigningRule(),
        new UapProtocolRule(),

        // RWS-REG-*
        new HklmWriteRule(),
        new WritableHandleRule(),
        new ElevationGuardRule(),
        new SetAccessControlRule(),

        // RWS-EXEC-*
        new ProcessStartRule(),
        new UseShellExecuteRule(),
        new AssemblyLoadRule(),
        new UriHandlerRule(),
        new ProcessStartInterpolationRule(),

        // RWS-SEC-*
        new ApiKeyPatternRule(),
        new ResourceSecretRule(),
        new ProjectMetadataSecretRule(),
        new ConnectionStringInCodeRule(),

        // RWS-XAML-*
        new XamlReaderRule(),
        new ResourceDictionaryRule(),
        new XamlCodeElementRule(),

        // RWS-PINVOKE-*
        new DllImportCharSetRule(),
        new DllImportDllNameRule(),
        new DangerousApiRule(),

        // RWS-ACL-*
        new FileAclRule(),
        new OpenAccessRuleRule(),

        // RWS-BIN-*
        new BinarySecretRule(),
        new BinaryConnectionStringRule(),

        // RWS-PY-*
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

        // RWS-JV-*
        new JavaCommandExecutionRule(),
        new JavaInsecureDeserializationRule(),
        new JavaWeakCryptoRule(),
        new JavaPathTraversalRule(),
        new JavaXmlXxeRule(),
        new JavaHardcodedSecretRule(),
        new JavaSqlInjectionRule(),
        new JavaSilentFailureRule(),
        new JavaUnboundedLoopRule(),

        // RWS-TS-*
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

        // RWS-CPP-*
        new CppUnsafeStringFunctionRule(),
        new CppMemoryLeakRule(),
        new CppCStyleCastRule(),
        new CppCommandInjectionRule(),
        new CppUsingNamespaceStdRule(),

        // RWS-DT-*
        new DartDynamicCodeRule(),
        new DartHardcodedSecretRule(),
        new DartSilentFailureRule(),
        new DartPrintStatementRule(),

        // RWS-GO-*
        new GoCommandInjectionRule(),
        new GoHardcodedSecretRule(),
        new GoSqlInjectionRule(),

        // RWS-KT-*
        new KotlinHardcodedSecretRule(),
        new KotlinSilentFailureRule(),
        new KotlinForceUnwrapRule(),
        new KotlinSqlInjectionRule(),

        // RWS-PL-*
        new PerlCommandInjectionRule(),
        new PerlHardcodedSecretRule(),

        // RWS-PHP-*
        new PhpCommandInjectionRule(),
        new PhpSqlInjectionRule(),

        // RWS-RS-*
        new RustUnsafeBlockRule(),
        new RustUnwrapRule(),

        // RWS-SW-*
        new SwiftForceUnwrapRule(),
        new SwiftSilentFailureRule(),

        // RWS-WEB-*
        new WebMixedContentRule(),

        // RWS-COM-*
        new TodoFixmeRule(),
    ];
}
