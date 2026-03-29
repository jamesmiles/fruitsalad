-- @test type-003: Recursive Medley for Expressions
-- @expect 20

medley Expr {
    Num(value),
    Add(left, right),
    Mul(left, right),
}

blend eval(expr) {
    sort expr {
        Expr.Num(v) => v
        Expr.Add(l, r) => eval(l) + eval(r)
        Expr.Mul(l, r) => eval(l) * eval(r)
    }
}

blend main() {
    -- Mul(Add(Num(2), Num(3)), Num(4)) = (2 + 3) * 4 = 20
    preserve expr = Expr.Mul(
        Expr.Add(Expr.Num(2), Expr.Num(3)),
        Expr.Num(4)
    )
    display(eval(expr))
}
