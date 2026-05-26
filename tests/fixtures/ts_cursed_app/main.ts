import { exec } from 'child_process';
import * as crypto from 'crypto';

function processInput(userInput: string) {
    // RSH-TS-001: TsDangerousEvalRule
    eval("console.log(" + userInput + ")");
    setTimeout("alert(" + userInput + ")", 500);

    // RSH-TS-002: TsCommandExecutionRule
    exec(`cat ${userInput}`);

    // RSH-TS-003: TsInsecureCryptoRule
    const hash = crypto.createHash('sha1');

    // RSH-TS-004: TsPrototypePollutionRule
    const dynamicKey = "__proto__";
    const myObj = {};
    (myObj as any)[dynamicKey] = "polluted";

    // RSH-TS-005: TsReactHtmlInjectionRule
    const htmlSnippet = "<div dangerouslySetInnerHTML={{__html: userInput}} />";
    const elem = document.getElementById("mydiv")!;
    elem.innerHTML = userInput;
    
    // RSH-TS-006: Hardcoded secret
    const api_key = "some_literal_string_key";
    
    // RSH-TS-007: SQL Injection
    const query = `SELECT * FROM users WHERE username = ${userInput}`;
    
    // RSH-TS-008: Silent failure
    try {
        const x = 1;
    } catch (e) {
        // Do nothing
    }
    
    // RSH-TS-009: Unbounded loop
    while (true) {
        console.log("Loop");
    }
    
    // RSH-TS-010: Missing timeout
    fetch("http://example.com");
    
    // RSH-COM-001: TODO
    // XXX: fix this
}
