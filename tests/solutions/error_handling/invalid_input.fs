-- @test err-002: Input Validation with Error Handling
-- @expect ok(42)
-- @expect error("invalid integer: hello")
-- @expect error("empty input")
-- @expect ok(-17)
-- @expect error("invalid integer: 12.5")

blend is_digit(ch) {
    ch == "0" || ch == "1" || ch == "2" || ch == "3" || ch == "4" ||
    ch == "5" || ch == "6" || ch == "7" || ch == "8" || ch == "9"
}

blend parse_int(s) {
    if s.len() == 0 {
        yield rot("empty input")
    }

    fresh start = 0
    fresh negative = false

    if s[0] == "-" {
        if s.len() == 1 {
            yield rot("invalid integer: {s}")
        }
        negative = true
        start = 1
    }

    -- Check all remaining chars are digits
    each i in start..s.len() {
        if !is_digit(s[i]) {
            yield rot("invalid integer: {s}")
        }
    }

    -- Build number manually
    fresh result = 0
    each i in start..s.len() {
        fresh d = 0
        if s[i] == "0" { d = 0 }
        if s[i] == "1" { d = 1 }
        if s[i] == "2" { d = 2 }
        if s[i] == "3" { d = 3 }
        if s[i] == "4" { d = 4 }
        if s[i] == "5" { d = 5 }
        if s[i] == "6" { d = 6 }
        if s[i] == "7" { d = 7 }
        if s[i] == "8" { d = 8 }
        if s[i] == "9" { d = 9 }
        result = result * 10 + d
    }

    if negative {
        result = 0 - result
    }

    ripe(result)
}

blend format_result(result) {
    sort result {
        ripe(v) => "ok({v})"
        rot(e) => "error(\"{e}\")"
    }
}

blend main() {
    display(format_result(parse_int("42")))
    display(format_result(parse_int("hello")))
    display(format_result(parse_int("")))
    display(format_result(parse_int("-17")))
    display(format_result(parse_int("12.5")))
}
