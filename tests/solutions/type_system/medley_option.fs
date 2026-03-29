-- @test type-002: Option Type with Medley
-- @expect Some: 5
-- @expect None

medley Maybe {
    Some(value),
    None,
}

blend safe_divide(a, b) {
    if b == 0 {
        yield Maybe.None
    }
    Maybe.Some(a / b)
}

blend show_result(result) {
    sort result {
        Maybe.Some(v) => {
            display("Some: {v}")
        }
        Maybe.None => {
            display("None")
        }
    }
}

blend main() {
    show_result(safe_divide(10, 2))
    show_result(safe_divide(10, 0))
}
