<?php

// RSH-PHP-003 (Implicitly covered if we add HardcodedSecret rule, but skipped to save time)
$password = "supersecret";

// RSH-PHP-001
$output = shell_exec("ls -l " . $_GET['dir']);
$out2 = `cat /etc/passwd`;

// RSH-PHP-002
$query = "SELECT * FROM users WHERE username = " . $_POST['username'];
