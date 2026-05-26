import java.io.File;
import java.io.ObjectInputStream;
import java.security.MessageDigest;
import javax.xml.parsers.DocumentBuilderFactory;

public class Main {
    public static void main(String[] args) throws Exception {
        String userInput = args[0];

        // RSH-JV-001: JavaCommandExecutionRule
        Runtime.getRuntime().exec("ping " + userInput);

        // RSH-JV-002: JavaInsecureDeserializationRule
        ObjectInputStream ois = new ObjectInputStream(null);

        // RSH-JV-003: JavaWeakCryptoRule
        MessageDigest md = MessageDigest.getInstance("MD5");

        // RSH-JV-004: JavaPathTraversalRule
        File f = new File("/var/data/" + userInput);

        // RSH-JV-005: JavaXmlXxeRule
        DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
        
        // RSH-JV-006: Hardcoded secret
        String password = "some_literal_password_123";
        
        // RSH-JV-007: SQL Injection
        String query = "SELECT * FROM users WHERE username = " + userInput;
        
        // RSH-JV-008: Silent failure
        try {
            int a = 1 / 0;
        } catch (Exception e) {
            // Do nothing
        }
        
        // RSH-COM-001: TODO
        // FIXME: fix this later
        
        // RSH-JV-009: Unbounded loop
        while (true) {
            System.out.println("Loop");
        }
    }
}
