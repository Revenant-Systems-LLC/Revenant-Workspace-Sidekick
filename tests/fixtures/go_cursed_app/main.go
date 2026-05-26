package main

import (
	"fmt"
	"os/exec"
)

func main() {
	// RSH-GO-002
	password := "supersecret"

	userInput := "some_input"
	// RSH-GO-001
	cmd := exec.Command("bash", "-c", userInput)
	cmd.Run()

	// RSH-GO-004
	query := fmt.Sprintf("SELECT * FROM users WHERE username = '%s'", userInput)
	fmt.Println(query, password)
}
