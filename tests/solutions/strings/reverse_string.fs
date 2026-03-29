-- @test string-001: Reverse String
-- @expect olleh

blend reverse(s: Banana) -> Banana {
    fresh result = ""
    fresh i = s.len() - 1
    while i >= 0 {
        result = result + s[i]
        i = i - 1
    }
    result
}

blend main() {
    display(reverse("hello"))
}
