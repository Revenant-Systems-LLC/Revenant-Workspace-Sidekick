fn main() {
    // RSH-RS-001
    unsafe {
        let x = 5;
    }

    // RSH-RS-002
    let val: Option<i32> = None;
    val.unwrap();
}
