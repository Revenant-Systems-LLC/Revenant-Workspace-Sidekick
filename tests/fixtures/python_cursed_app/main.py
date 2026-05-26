import os
import subprocess
import pickle
import hashlib
import yaml
from flask import Flask
from flask_cors import CORS
# RSH-PY-014: Wildcard import
from math import *

app = Flask(__name__)
# RSH-PY-005: Hardcoded SECRET_KEY and CORS *
CORS(app, origins="*")
app.config['SECRET_KEY'] = 'vuln-default-secret-key-12345'

@app.route('/run')
def run_command():
    user_data = "ls"
    # RSH-PY-001: Dangerous dynamic evaluation
    eval(f"print('{user_data}')")

    # RSH-PY-002: Dangerous shell/subprocess
    subprocess.run("ping " + user_data, shell=True)
    os.system("echo hello")

    # RSH-PY-003: Insecure deserialization
    untrusted_payload = b"\x80\x04\x95..."
    pickle.loads(untrusted_payload)
    yaml.unsafe_load("!!python/object/apply:os.system ['echo vuln']")

    # RSH-PY-004: Weak crypt
    weak_hash = hashlib.md5(b"password").hexdigest()

    # RSH-PY-006: Hardcoded secret
    api_key = "some_literal_string_secret"

    # RSH-PY-007: SQL Injection
    query = f"SELECT * FROM users WHERE id = {user_data}"
    
    # RSH-PY-008: Silent failure
    try:
        pass
    except Exception:
        pass
        
    # RSH-COM-001: TODO
    # TODO: remove this later
    
    # RSH-PY-009: Missing timeout
    import requests
    requests.get("http://example.com")
    
    # RSH-PY-010: Unbounded loop
    while True:
        print("Infinite")

    # RSH-PY-011: Hardcoded absolute path
    f = open("C:\\Users\\Dave\\Desktop\\grades.csv")

    # RSH-PY-012: Shadowing a built-in
    list = [1, 2, 3]

    # RSH-PY-013: Global keyword
    global counter
    counter = counter + 1

    # RSH-PY-016: Deep nesting
    if True:
        for n in range(10):
            for m in range(10):
                if n > m:
                    print("Too deep!")

    # RSH-PY-018: Bad naming
    x = 42
    data = "something"

    return weak_hash

if __name__ == '__main__':
    # RSH-PY-005: Debug mode in production run
    app.run(debug=True)
