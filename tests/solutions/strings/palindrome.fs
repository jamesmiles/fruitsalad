-- @test string-002: Palindrome Check
-- @expect true
-- @expect false

blend is_palindrome(s) {
    fresh left = 0
    fresh right = s.len() - 1
    fresh result = true
    while left < right {
        if s[left] != s[right] {
            result = false
            snap
        }
        left = left + 1
        right = right - 1
    }
    result
}

blend main() {
    display(is_palindrome("racecar"))
    display(is_palindrome("hello"))
}
