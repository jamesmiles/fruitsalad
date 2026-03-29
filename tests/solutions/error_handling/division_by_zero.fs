-- @test err-001: Safe Division with Error Handling
-- @expect ok(5)
-- @expect error("division by zero")
-- @expect ok(0)
-- @expect ok(-5)
-- @expect ok(3)

blend safe_div(a, b) {
    if b == 0 {
        yield rot("division by zero")
    }
    ripe(a / b)
}

blend format_result(result) {
    sort result {
        ripe(v) => "ok({v})"
        rot(e) => "error(\"{e}\")"
    }
}

blend main() {
    display(format_result(safe_div(10, 2)))
    display(format_result(safe_div(10, 0)))
    display(format_result(safe_div(0, 5)))
    display(format_result(safe_div(-15, 3)))
    display(format_result(safe_div(7, 2)))
}
