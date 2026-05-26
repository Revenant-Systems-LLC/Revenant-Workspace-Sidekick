#include <cstdlib>
#include <cstdio>
#include <cstring>

using namespace std;

class Main {
public:
    void process(const char* userInput) {
        // RSH-CPP-001: Unsafe string functions
        char buf[64];
        strcpy(buf, userInput);
        sprintf(buf, "Hello %s", userInput);

        // RSH-CPP-002: Raw memory allocation
        int* arr = new int[100];

        // RSH-CPP-003: C-style cast
        void* ptr = malloc(128);
        int* iptr = (int*)ptr;

        // RSH-CPP-004: Command injection
        system("ls -la");

        // RSH-CPP-005: using namespace std in header (tested via .h file below)
    }
};
