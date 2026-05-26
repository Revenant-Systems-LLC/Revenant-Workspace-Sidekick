import 'dart:mirrors';

class ApiService {
  final String apiKey = "sk-live-abc123def456";

  Future<void> fetchData() async {
    try {
      // some operation
    } catch (e) {
      // silent failure
    }

    print("fetching data...");
  }
}
