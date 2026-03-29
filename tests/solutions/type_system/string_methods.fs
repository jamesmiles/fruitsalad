-- @test type-009: String methods
-- @expect hello
-- @expect HELLO
-- @expect true
-- @expect Hey
-- @expect [a, b, c]
-- @expect spaces
-- @expect [h, i]
-- @expect true

blend main() {
    display("Hello".to_lower())
    display("hello".to_upper())
    display("Hello".starts_with("He"))
    display("Hello".replace("llo", "y"))
    display("a,b,c".split(","))
    display("  spaces  ".trim())
    display("hi".chars())
    display("hello world".contains("world"))
}
