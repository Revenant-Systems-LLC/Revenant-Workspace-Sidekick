func doSomething() {
    // RSH-SW-001
    var name: String? = nil
    print(name!)

    // RSH-SW-002
    let data = try? Data(contentsOf: URL(string: "http://example.com")!)
}
