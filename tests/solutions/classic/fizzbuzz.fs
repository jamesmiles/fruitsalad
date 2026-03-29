-- @test classic-002: FizzBuzz 1-15
-- @expect 1
-- @expect 2
-- @expect Fizz
-- @expect 4
-- @expect Buzz
-- @expect Fizz
-- @expect 7
-- @expect 8
-- @expect Fizz
-- @expect Buzz
-- @expect 11
-- @expect Fizz
-- @expect 13
-- @expect 14
-- @expect FizzBuzz

blend main() {
    each i in 1..=15 {
        if i % 15 == 0 {
            display("FizzBuzz")
        } else if i % 3 == 0 {
            display("Fizz")
        } else if i % 5 == 0 {
            display("Buzz")
        } else {
            display(i)
        }
    }
}
