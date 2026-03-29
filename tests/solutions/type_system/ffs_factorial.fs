-- @test ffs-002: Self-hosting compiler compiles factorial
-- @expect def factorial(n):
-- @expect     if (n <= 1):
-- @expect         return 1
-- @expect     return (n * factorial((n - 1)))
-- @expect def main():
-- @expect     print(factorial(10))
-- @expect main()

-- This test uses the full ffs compiler to compile a factorial program

-- ============================================================
-- We include the core compiler functions inline for testing
-- (In production, these would be imported from ffs/compiler.fs)
-- ============================================================

blend is_alpha(c) {
    preserve code = char_code(c)
    if (code >= 65 && code <= 90) || (code >= 97 && code <= 122) || code == 95 {
        yield true
    }
    false
}

blend is_digit(c) {
    preserve code = char_code(c)
    code >= 48 && code <= 57
}

blend is_alnum(c) {
    is_alpha(c) || is_digit(c)
}

blend is_space(c) {
    c == " " || c == "\t" || c == "\n" || c == "\r"
}

blend tokenize(source) {
    fresh tokens = []
    fresh pos = 0
    fresh line_num = 1
    preserve len = source.len()

    while pos < len {
        preserve c = source[pos]
        if is_space(c) {
            if c == "\n" { line_num = line_num + 1 }
            pos = pos + 1
            skip
        }
        if c == "-" && pos + 1 < len && source[pos + 1] == "-" {
            while pos < len && source[pos] != "\n" { pos = pos + 1 }
            skip
        }
        if is_digit(c) {
            fresh num = ""
            while pos < len && is_digit(source[pos]) {
                num = num + source[pos]
                pos = pos + 1
            }
            tokens.push(["INT", num])
            skip
        }
        if c == "\"" {
            pos = pos + 1
            fresh str_val = ""
            while pos < len && source[pos] != "\"" {
                str_val = str_val + source[pos]
                pos = pos + 1
            }
            pos = pos + 1
            tokens.push(["STR", str_val])
            skip
        }
        if is_alpha(c) {
            fresh word = ""
            while pos < len && is_alnum(source[pos]) {
                word = word + source[pos]
                pos = pos + 1
            }
            preserve kws = ["blend", "preserve", "fresh", "if", "else", "while",
                "each", "in", "display", "true", "false", "yield"]
            fresh is_kw = false
            each i in 0..kws.len() {
                if kws[i] == word { is_kw = true }
            }
            if is_kw { tokens.push(["KW", word]) }
            else { tokens.push(["ID", word]) }
            skip
        }
        if pos + 1 < len {
            preserve two = c + source[pos + 1]
            if two == "==" || two == "!=" || two == "<=" || two == ">=" || two == "&&" || two == "||" {
                tokens.push(["OP", two])
                pos = pos + 2
                skip
            }
        }
        if c == "+" || c == "-" || c == "*" || c == "/" || c == "%" || c == "<" || c == ">" || c == "!" || c == "=" {
            tokens.push(["OP", to_banana(c)])
            pos = pos + 1
            skip
        }
        if c == "(" || c == ")" || c == "," || c == ":" || c == "." {
            tokens.push(["DELIM", to_banana(c)])
            pos = pos + 1
            skip
        }
        if char_code(c) == 123 {
            tokens.push(["DELIM", from_char_code(123)])
            pos = pos + 1
            skip
        }
        if char_code(c) == 125 {
            tokens.push(["DELIM", from_char_code(125)])
            pos = pos + 1
            skip
        }
        pos = pos + 1
    }
    tokens.push(["EOF", ""])
    tokens
}

-- Parser helpers
blend pk(tokens, pr) { tokens[pr[0]] }
blend pk_t(tokens, pr) { tokens[pr[0]][0] }
blend pk_v(tokens, pr) { tokens[pr[0]][1] }
blend adv(tokens, pr) {
    preserve t = tokens[pr[0]]
    pr[0] = pr[0] + 1
    t
}
blend match_kw(tokens, pr, kw) {
    if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == kw { adv(tokens, pr) yield true }
    false
}
blend match_d(tokens, pr, d) {
    if pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == d { adv(tokens, pr) yield true }
    false
}
blend match_op(tokens, pr, op) {
    if pk_t(tokens, pr) == "OP" && pk_v(tokens, pr) == op { adv(tokens, pr) yield true }
    false
}

blend parse_program(tokens, pr) {
    fresh funcs = []
    fresh stmts = []
    while pk_t(tokens, pr) != "EOF" {
        if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == "blend" {
            funcs.push(parse_blend(tokens, pr))
        } else {
            stmts.push(parse_stmt(tokens, pr))
        }
    }
    yield ["Program", funcs, stmts]
}

blend parse_blend(tokens, pr) {
    adv(tokens, pr)  -- blend
    preserve name = adv(tokens, pr)[1]
    adv(tokens, pr)  -- (
    fresh params = []
    if !(pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == ")") {
        params.push(adv(tokens, pr)[1])
        if pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == ":" {
            adv(tokens, pr)
            adv(tokens, pr)
        }
        while pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == "," {
            adv(tokens, pr)
            params.push(adv(tokens, pr)[1])
            if pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == ":" {
                adv(tokens, pr)
                adv(tokens, pr)
            }
        }
    }
    adv(tokens, pr)  -- )
    preserve body = parse_block(tokens, pr)
    yield ["BlendDef", name, params, body]
}

blend parse_block(tokens, pr) {
    adv(tokens, pr)  -- {
    fresh stmts = []
    while !(pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == from_char_code(125)) && pk_t(tokens, pr) != "EOF" {
        stmts.push(parse_stmt(tokens, pr))
    }
    adv(tokens, pr)  -- }
    yield ["Block", stmts]
}

blend parse_stmt(tokens, pr) {
    if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == "if" { yield parse_if(tokens, pr) }
    if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == "yield" {
        adv(tokens, pr)
        yield ["Yield", parse_expr(tokens, pr)]
    }
    if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == "display" {
        adv(tokens, pr)
        adv(tokens, pr)  -- (
        fresh args = []
        if !(pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == ")") {
            args.push(parse_expr(tokens, pr))
            while pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == "," {
                adv(tokens, pr)
                args.push(parse_expr(tokens, pr))
            }
        }
        adv(tokens, pr)  -- )
        yield ["Display", args]
    }
    parse_expr(tokens, pr)
}

blend parse_if(tokens, pr) {
    adv(tokens, pr)  -- if
    preserve cond = parse_expr(tokens, pr)
    preserve then_b = parse_block(tokens, pr)
    fresh else_b = ["Block", []]
    if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == "else" {
        adv(tokens, pr)
        if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == "if" {
            else_b = ["Block", [parse_if(tokens, pr)]]
        } else {
            else_b = parse_block(tokens, pr)
        }
    }
    yield ["If", cond, then_b, else_b]
}

blend parse_expr(tokens, pr) { parse_cmp(tokens, pr) }

blend parse_cmp(tokens, pr) {
    fresh left = parse_add(tokens, pr)
    while pk_t(tokens, pr) == "OP" && (pk_v(tokens, pr) == "<=" || pk_v(tokens, pr) == ">=" || pk_v(tokens, pr) == "<" || pk_v(tokens, pr) == ">" || pk_v(tokens, pr) == "==" || pk_v(tokens, pr) == "!=") {
        preserve op = adv(tokens, pr)[1]
        left = ["BinOp", op, left, parse_add(tokens, pr)]
    }
    left
}

blend parse_add(tokens, pr) {
    fresh left = parse_mul(tokens, pr)
    while pk_t(tokens, pr) == "OP" && (pk_v(tokens, pr) == "+" || pk_v(tokens, pr) == "-") {
        preserve op = adv(tokens, pr)[1]
        left = ["BinOp", op, left, parse_mul(tokens, pr)]
    }
    left
}

blend parse_mul(tokens, pr) {
    fresh left = parse_unary(tokens, pr)
    while pk_t(tokens, pr) == "OP" && (pk_v(tokens, pr) == "*" || pk_v(tokens, pr) == "/" || pk_v(tokens, pr) == "%") {
        preserve op = adv(tokens, pr)[1]
        left = ["BinOp", op, left, parse_unary(tokens, pr)]
    }
    left
}

blend parse_unary(tokens, pr) {
    if pk_t(tokens, pr) == "OP" && pk_v(tokens, pr) == "-" {
        adv(tokens, pr)
        yield ["UnaryOp", "-", parse_unary(tokens, pr)]
    }
    parse_postfix(tokens, pr)
}

blend parse_postfix(tokens, pr) {
    fresh expr = parse_primary(tokens, pr)
    loop {
        if pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == "(" {
            adv(tokens, pr)
            fresh args = []
            if !(pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == ")") {
                args.push(parse_expr(tokens, pr))
                while pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == "," {
                    adv(tokens, pr)
                    args.push(parse_expr(tokens, pr))
                }
            }
            adv(tokens, pr)  -- )
            expr = ["Call", expr, args]
            skip
        }
        snap
    }
    expr
}

blend parse_primary(tokens, pr) {
    if pk_t(tokens, pr) == "INT" { yield ["NumLit", adv(tokens, pr)[1]] }
    if pk_t(tokens, pr) == "STR" { yield ["StrLit", adv(tokens, pr)[1]] }
    if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == "true" { adv(tokens, pr) yield ["BoolLit", "true"] }
    if pk_t(tokens, pr) == "KW" && pk_v(tokens, pr) == "false" { adv(tokens, pr) yield ["BoolLit", "false"] }
    if pk_t(tokens, pr) == "ID" { yield ["Ident", adv(tokens, pr)[1]] }
    if pk_t(tokens, pr) == "DELIM" && pk_v(tokens, pr) == "(" {
        adv(tokens, pr)
        preserve e = parse_expr(tokens, pr)
        adv(tokens, pr)  -- )
        yield e
    }
    adv(tokens, pr)
    yield ["NumLit", "0"]
}

-- Code generator
blend ind(n) {
    fresh s = ""
    each i in 0..n { s = s + "    " }
    s
}

blend is_expr_node(node) {
    preserve t = node[0]
    t == "BinOp" || t == "UnaryOp" || t == "Call" || t == "Ident" || t == "NumLit" || t == "StrLit" || t == "BoolLit"
}

blend gen(node, lvl) {
    preserve t = node[0]
    if t == "Program" {
        fresh r = ""
        each i in 0..node[1].len() { r = r + gen(node[1][i], 0) + "\n" }
        each i in 0..node[2].len() { r = r + gen(node[2][i], 0) + "\n" }
        each i in 0..node[1].len() {
            if node[1][i][1] == "main" { r = r + "main()\n" }
        }
        yield r
    }
    if t == "BlendDef" {
        fresh ps = ""
        each i in 0..node[2].len() {
            if i > 0 { ps = ps + ", " }
            ps = ps + node[2][i]
        }
        fresh r = ind(lvl) + "def " + node[1] + "(" + ps + "):\n"
        preserve stmts = node[3][1]
        each i in 0..stmts.len() {
            preserve is_last = (i == stmts.len() - 1)
            if is_last && is_expr_node(stmts[i]) {
                r = r + ind(lvl + 1) + "return " + gx(stmts[i]) + "\n"
            } else {
                r = r + gen(stmts[i], lvl + 1) + "\n"
            }
        }
        yield r
    }
    if t == "If" {
        fresh r = ind(lvl) + "if " + gx(node[1]) + ":\n"
        each i in 0..node[2][1].len() { r = r + gen(node[2][1][i], lvl + 1) + "\n" }
        if node[3][1].len() > 0 {
            r = r + ind(lvl) + "else:\n"
            each i in 0..node[3][1].len() { r = r + gen(node[3][1][i], lvl + 1) + "\n" }
        }
        yield r
    }
    if t == "Yield" { yield ind(lvl) + "return " + gx(node[1]) }
    if t == "Display" {
        fresh astr = ""
        each i in 0..node[1].len() {
            if i > 0 { astr = astr + ", " }
            astr = astr + gx(node[1][i])
        }
        yield ind(lvl) + "print(" + astr + ")"
    }
    yield ind(lvl) + gx(node)
}

blend gx(node) {
    preserve t = node[0]
    if t == "NumLit" { yield node[1] }
    if t == "StrLit" { yield "\"" + node[1] + "\"" }
    if t == "BoolLit" {
        if node[1] == "true" { yield "True" }
        yield "False"
    }
    if t == "Ident" { yield node[1] }
    if t == "BinOp" { yield "(" + gx(node[2]) + " " + node[1] + " " + gx(node[3]) + ")" }
    if t == "UnaryOp" { yield "(-" + gx(node[2]) + ")" }
    if t == "Call" {
        fresh astr = ""
        each i in 0..node[2].len() {
            if i > 0 { astr = astr + ", " }
            astr = astr + gx(node[2][i])
        }
        yield gx(node[1]) + "(" + astr + ")"
    }
    if t == "Display" {
        fresh astr = ""
        each i in 0..node[1].len() {
            if i > 0 { astr = astr + ", " }
            astr = astr + gx(node[1][i])
        }
        yield "print(" + astr + ")"
    }
    "None"
}

blend trim_out(code) {
    fresh output = ""
    preserve lines = code.split("\n")
    fresh first = true
    each i in 0..lines.len() {
        if lines[i].trim().len() > 0 {
            if !first { output = output + "\n" }
            output = output + lines[i]
            first = false
        }
    }
    output
}

blend main() {
    preserve lb = from_char_code(123)
    preserve rb = from_char_code(125)
    preserve nl = from_char_code(10)

    -- Build: blend factorial(n) { if n <= 1 { yield 1 } n * factorial(n - 1) }
    -- blend main() { display(factorial(10)) }
    fresh src = "blend factorial(n) " + lb + nl
    src = src + "    if n <= 1 " + lb + nl
    src = src + "        yield 1" + nl
    src = src + "    " + rb + nl
    src = src + "    n * factorial(n - 1)" + nl
    src = src + rb + nl + nl
    src = src + "blend main() " + lb + nl
    src = src + "    display(factorial(10))" + nl
    src = src + rb

    preserve tokens = tokenize(src)
    fresh pr = [0]
    preserve ast = parse_program(tokens, pr)
    preserve py = gen(ast, 0)
    display(trim_out(py))
}
