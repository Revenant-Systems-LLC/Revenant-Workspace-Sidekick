class Main {
    fun doSomething(userInput: String) {
        // RSH-KT-001
        val apiKey = "some_secret_key"

        // RSH-KT-002
        try {
            print(apiKey)
        } catch (e: Exception) {
        }

        // RSH-KT-003
        var str: String? = null
        print(str!!)

        // RSH-KT-004
        val query = "SELECT * FROM users WHERE name = '${userInput}'"
        db.query(query)
    }
}
