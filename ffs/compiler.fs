-- ffs: Fruit Salad compiler written in Fruit Salad
-- Transpiles a subset of Fruit Salad to Python

-- ============================================================
-- TOKENIZER HELPERS
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
    if code >= 48 && code <= 57 {
        yield true
    }
    false
}

blend is_alnum(c) {
    if is_alpha(c) || is_digit(c) {
        yield true
    }
    false
}

blend is_whitespace(c) {
    if c == " " || c == "\t" || c == "\n" || c == "\r" {
        yield true
    }
    false
}

-- make a token: [type, value, line]
blend mk_tok(typ, val, line) {
    [typ, val, line]
}

-- ============================================================
-- KEYWORDS
-- ============================================================

blend is_keyword(word) {
    if word == "blend" || word == "preserve" || word == "fresh" {
        yield true
    }
    if word == "if" || word == "else" || word == "while" {
        yield true
    }
    if word == "each" || word == "in" || word == "loop" {
        yield true
    }
    if word == "snap" || word == "skip" || word == "yield" {
        yield true
    }
    if word == "display" || word == "true" || word == "false" {
        yield true
    }
    if word == "candied" {
        yield true
    }
    if word == "peel" || word == "abs" || word == "min" || word == "max" {
        yield true
    }
    if word == "to_apple" || word == "to_date" || word == "to_banana" {
        yield true
    }
    if word == "char_code" || word == "from_char_code" || word == "sqrt" {
        yield true
    }
    if word == "read_file" || word == "read_stdin" || word == "args" {
        yield true
    }
    if word == "bowl" || word == "medley" || word == "sort" {
        yield true
    }
    if word == "ripe" || word == "rot" || word == "pit" || word == "toss" {
        yield true
    }
    false
}

-- ============================================================
-- TOKENIZER
-- ============================================================

blend tokenize(source) {
    fresh tokens = []
    fresh pos = 0
    fresh line = 1
    preserve slen = source.len()

    loop {
        if pos >= slen {
            snap
        }

        fresh c = source[pos]

        -- skip whitespace
        if is_whitespace(c) {
            if c == "\n" {
                line = line + 1
            }
            pos = pos + 1
            skip
        }

        -- line comment --
        if c == "-" && pos + 1 < slen && source[pos + 1] == "-" {
            pos = pos + 2
            loop {
                if pos >= slen || source[pos] == "\n" {
                    snap
                }
                pos = pos + 1
            }
            skip
        }

        -- block comment -{ }-
        if c == "-" && pos + 1 < slen && source[pos + 1] == "\{" {
            pos = pos + 2
            fresh depth = 1
            loop {
                if pos >= slen || depth == 0 {
                    snap
                }
                if pos + 1 < slen && source[pos] == "-" && source[pos + 1] == "\{" {
                    depth = depth + 1
                    pos = pos + 2
                    skip
                }
                if pos + 1 < slen && source[pos] == "\}" && source[pos + 1] == "-" {
                    depth = depth - 1
                    pos = pos + 2
                    skip
                }
                if source[pos] == "\n" {
                    line = line + 1
                }
                pos = pos + 1
            }
            skip
        }

        -- string literal
        if c == "\"" {
            pos = pos + 1
            fresh buf = ""
            fresh has_interp = false
            fresh segments = []

            loop {
                if pos >= slen || source[pos] == "\"" {
                    snap
                }
                if source[pos] == "\\" && pos + 1 < slen {
                    pos = pos + 1
                    fresh esc = source[pos]
                    if esc == "n" {
                        buf = buf + "\n"
                    } else if esc == "t" {
                        buf = buf + "\t"
                    } else if esc == "\\" {
                        buf = buf + "\\"
                    } else if esc == "\"" {
                        buf = buf + "\""
                    } else if esc == "\{" {
                        buf = buf + "\{"
                    } else {
                        buf = buf + esc
                    }
                    pos = pos + 1
                    skip
                }
                if source[pos] == "\{" {
                    has_interp = true
                    segments.push(["STR_SEG", buf])
                    buf = ""
                    pos = pos + 1
                    -- read expression text until matching }
                    fresh expr_text = ""
                    fresh brace_depth = 1
                    loop {
                        if pos >= slen || brace_depth == 0 {
                            snap
                        }
                        if source[pos] == "\{" {
                            brace_depth = brace_depth + 1
                        }
                        if source[pos] == "\}" {
                            brace_depth = brace_depth - 1
                            if brace_depth == 0 {
                                pos = pos + 1
                                snap
                            }
                        }
                        expr_text = expr_text + source[pos]
                        pos = pos + 1
                    }
                    segments.push(["EXPR_SEG", expr_text])
                    skip
                }
                buf = buf + source[pos]
                pos = pos + 1
            }
            if pos < slen {
                pos = pos + 1
            }
            if has_interp {
                segments.push(["STR_SEG", buf])
                tokens.push(mk_tok("INTERP", segments, line))
            } else {
                tokens.push(mk_tok("STR", buf, line))
            }
            skip
        }

        -- number (integer or float)
        if is_digit(c) {
            fresh num_str = ""
            fresh has_dot = false
            loop {
                if pos >= slen {
                    snap
                }
                if source[pos] == "." && !has_dot && pos + 1 < slen && is_digit(source[pos + 1]) {
                    has_dot = true
                    num_str = num_str + source[pos]
                    pos = pos + 1
                    skip
                }
                if !is_digit(source[pos]) {
                    snap
                }
                num_str = num_str + source[pos]
                pos = pos + 1
            }
            if has_dot {
                tokens.push(mk_tok("NUM", num_str, line))
            } else {
                tokens.push(mk_tok("NUM", to_apple(num_str), line))
            }
            skip
        }

        -- identifier or keyword
        if is_alpha(c) {
            fresh word = ""
            loop {
                if pos >= slen {
                    snap
                }
                if !is_alnum(source[pos]) {
                    snap
                }
                word = word + source[pos]
                pos = pos + 1
            }
            if is_keyword(word) {
                tokens.push(mk_tok("KW", word, line))
            } else {
                tokens.push(mk_tok("ID", word, line))
            }
            skip
        }

        -- three-char operators first
        if pos + 2 < slen {
            fresh three = c + source[pos + 1] + source[pos + 2]
            if three == "..=" {
                tokens.push(mk_tok("OP", "..=", line))
                pos = pos + 3
                skip
            }
        }

        -- two-char operators
        if pos + 1 < slen {
            fresh two = c + source[pos + 1]
            if two == "==" || two == "!=" || two == "<=" || two == ">=" || two == "&&" || two == "||" || two == ".." || two == "->" || two == "~>" || two == "=>" {
                tokens.push(mk_tok("OP", two, line))
                pos = pos + 2
                skip
            }
        }

        -- single-char operators and delimiters
        if c == "(" || c == ")" || c == "\{" || c == "\}" || c == "[" || c == "]" {
            tokens.push(mk_tok("DELIM", c, line))
            pos = pos + 1
            skip
        }
        if c == "+" || c == "-" || c == "*" || c == "/" || c == "%" || c == "=" || c == "<" || c == ">" || c == "!" {
            tokens.push(mk_tok("OP", c, line))
            pos = pos + 1
            skip
        }
        if c == "," || c == ":" || c == "." {
            tokens.push(mk_tok("DELIM", c, line))
            pos = pos + 1
            skip
        }
        if c == "|" {
            tokens.push(mk_tok("DELIM", c, line))
            pos = pos + 1
            skip
        }

        -- unknown char, skip
        pos = pos + 1
    }

    tokens.push(mk_tok("EOF", "", line))
    tokens
}

-- ============================================================
-- PARSER
-- ============================================================

-- AST nodes are baskets: [type, ...data]
-- Using a mutable basket for parser position: pos_ref = [0]

blend parser_peek(tokens, pos_ref) {
    tokens[pos_ref[0]]
}

blend parser_advance(tokens, pos_ref) {
    preserve tok = tokens[pos_ref[0]]
    pos_ref[0] = pos_ref[0] + 1
    tok
}

blend parser_expect(tokens, pos_ref, typ, val) {
    preserve tok = parser_peek(tokens, pos_ref)
    if val != "" {
        if tok[0] != typ || tok[1] != val {
            display("Parse error: expected " + typ + " '" + val + "' but got " + tok[0] + " '" + to_banana(tok[1]) + "' at line " + to_banana(tok[2]))
        }
    } else {
        if tok[0] != typ {
            display("Parse error: expected " + typ + " but got " + tok[0] + " at line " + to_banana(tok[2]))
        }
    }
    parser_advance(tokens, pos_ref)
}

blend parser_match_kw(tokens, pos_ref, kw) {
    preserve tok = parser_peek(tokens, pos_ref)
    if tok[0] == "KW" && tok[1] == kw {
        parser_advance(tokens, pos_ref)
        yield true
    }
    false
}

blend parser_match_op(tokens, pos_ref, op) {
    preserve tok = parser_peek(tokens, pos_ref)
    if tok[0] == "OP" && tok[1] == op {
        parser_advance(tokens, pos_ref)
        yield true
    }
    false
}

blend parser_match_delim(tokens, pos_ref, d) {
    preserve tok = parser_peek(tokens, pos_ref)
    if tok[0] == "DELIM" && tok[1] == d {
        parser_advance(tokens, pos_ref)
        yield true
    }
    false
}

blend parser_at_end(tokens, pos_ref) {
    preserve tok = parser_peek(tokens, pos_ref)
    tok[0] == "EOF"
}

-- Parse the full program
blend parse(tokens) {
    fresh pos_ref = [0]
    fresh functions = []
    fresh stmts = []

    loop {
        if parser_at_end(tokens, pos_ref) {
            snap
        }
        preserve tok = parser_peek(tokens, pos_ref)
        if tok[0] == "KW" && tok[1] == "blend" {
            functions.push(parse_blend_def(tokens, pos_ref))
        } else if tok[0] == "KW" && tok[1] == "bowl" {
            stmts.push(parse_bowl_def(tokens, pos_ref))
        } else if tok[0] == "KW" && tok[1] == "medley" {
            stmts.push(parse_medley_def(tokens, pos_ref))
        } else {
            stmts.push(parse_statement(tokens, pos_ref))
        }
    }

    yield ["Program", functions, stmts]
}

-- Parse a blend definition
blend parse_blend_def(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "KW", "blend")
    -- Accept both ID and KW as function name (e.g. blend char_code(c))
    preserve name_tok = parser_advance(tokens, pos_ref)
    preserve name = name_tok[1]
    parser_expect(tokens, pos_ref, "DELIM", "(")
    fresh params = []
    preserve peek0 = parser_peek(tokens, pos_ref)
    if !(peek0[0] == "DELIM" && peek0[1] == ")") {
        preserve p1 = parser_expect(tokens, pos_ref, "ID", "")
        params.push(p1[1])
        -- skip optional type annotation
        parse_skip_type_annotation(tokens, pos_ref)
        loop {
            if !parser_match_delim(tokens, pos_ref, ",") {
                snap
            }
            preserve pn = parser_expect(tokens, pos_ref, "ID", "")
            params.push(pn[1])
            parse_skip_type_annotation(tokens, pos_ref)
        }
    }
    parser_expect(tokens, pos_ref, "DELIM", ")")
    -- skip optional return type
    if parser_match_op(tokens, pos_ref, "->") {
        -- consume the type name
        parser_advance(tokens, pos_ref)
    }
    preserve body = parse_block(tokens, pos_ref)
    yield ["BlendDef", name, params, body]
}

blend parse_skip_type_annotation(tokens, pos_ref) {
    preserve tok = parser_peek(tokens, pos_ref)
    if tok[0] == "DELIM" && tok[1] == ":" {
        parser_advance(tokens, pos_ref)
        -- consume type name (identifier)
        parser_advance(tokens, pos_ref)
    }
}

-- Parse a block { ... }
blend parse_block(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "DELIM", "\{")
    fresh stmts = []
    loop {
        preserve tok = parser_peek(tokens, pos_ref)
        if tok[0] == "DELIM" && tok[1] == "\}" {
            snap
        }
        if tok[0] == "EOF" {
            snap
        }
        stmts.push(parse_statement(tokens, pos_ref))
    }
    parser_expect(tokens, pos_ref, "DELIM", "\}")
    yield ["Block", stmts]
}

-- Parse a statement
blend parse_statement(tokens, pos_ref) {
    preserve tok = parser_peek(tokens, pos_ref)

    if tok[0] == "KW" && tok[1] == "preserve" {
        yield parse_var_def(tokens, pos_ref, "preserve")
    }
    if tok[0] == "KW" && tok[1] == "fresh" {
        yield parse_var_def(tokens, pos_ref, "fresh")
    }
    if tok[0] == "KW" && tok[1] == "candied" {
        yield parse_var_def(tokens, pos_ref, "candied")
    }
    if tok[0] == "KW" && tok[1] == "if" {
        yield parse_if(tokens, pos_ref)
    }
    if tok[0] == "KW" && tok[1] == "while" {
        yield parse_while(tokens, pos_ref)
    }
    if tok[0] == "KW" && tok[1] == "each" {
        yield parse_each(tokens, pos_ref)
    }
    if tok[0] == "KW" && tok[1] == "loop" {
        yield parse_loop(tokens, pos_ref)
    }
    if tok[0] == "KW" && tok[1] == "snap" {
        parser_advance(tokens, pos_ref)
        yield ["Snap"]
    }
    if tok[0] == "KW" && tok[1] == "skip" {
        parser_advance(tokens, pos_ref)
        yield ["Skip"]
    }
    if tok[0] == "KW" && tok[1] == "yield" {
        parser_advance(tokens, pos_ref)
        preserve val = parse_expression(tokens, pos_ref)
        yield ["Yield", val]
    }
    if tok[0] == "KW" && tok[1] == "display" {
        parser_advance(tokens, pos_ref)
        parser_expect(tokens, pos_ref, "DELIM", "(")
        fresh args = []
        preserve peek1 = parser_peek(tokens, pos_ref)
        if !(peek1[0] == "DELIM" && peek1[1] == ")") {
            args.push(parse_expression(tokens, pos_ref))
            loop {
                if !parser_match_delim(tokens, pos_ref, ",") {
                    snap
                }
                args.push(parse_expression(tokens, pos_ref))
            }
        }
        parser_expect(tokens, pos_ref, "DELIM", ")")
        yield ["Display", args]
    }

    if tok[0] == "KW" && tok[1] == "sort" {
        yield parse_sort_expr(tokens, pos_ref)
    }
    if tok[0] == "KW" && tok[1] == "toss" {
        parser_advance(tokens, pos_ref)
        preserve toss_val = parse_expression(tokens, pos_ref)
        yield ["Toss", toss_val]
    }

    -- expression or assignment
    preserve expr = parse_expression(tokens, pos_ref)
    preserve next = parser_peek(tokens, pos_ref)
    if next[0] == "OP" && next[1] == "=" {
        parser_advance(tokens, pos_ref)
        preserve rhs = parse_expression(tokens, pos_ref)
        yield ["Assign", expr, rhs]
    }
    expr
}

-- Variable definition: preserve/fresh name = value
blend parse_var_def(tokens, pos_ref, kind) {
    parser_advance(tokens, pos_ref)
    preserve name_tok = parser_expect(tokens, pos_ref, "ID", "")
    -- skip optional type annotation
    parse_skip_type_annotation(tokens, pos_ref)
    parser_expect(tokens, pos_ref, "OP", "=")
    preserve value = parse_expression(tokens, pos_ref)
    yield ["VarDef", kind, name_tok[1], value]
}

-- If expression
blend parse_if(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "KW", "if")
    preserve cond = parse_expression(tokens, pos_ref)
    preserve then_block = parse_block(tokens, pos_ref)
    fresh else_block = ["Block", []]
    preserve next_tok = parser_peek(tokens, pos_ref)
    if next_tok[0] == "KW" && next_tok[1] == "else" {
        parser_advance(tokens, pos_ref)
        preserve peek2 = parser_peek(tokens, pos_ref)
        if peek2[0] == "KW" && peek2[1] == "if" {
            else_block = ["Block", [parse_if(tokens, pos_ref)]]
        } else {
            else_block = parse_block(tokens, pos_ref)
        }
    }
    yield ["If", cond, then_block, else_block]
}

-- While loop
blend parse_while(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "KW", "while")
    preserve cond = parse_expression(tokens, pos_ref)
    preserve body = parse_block(tokens, pos_ref)
    yield ["While", cond, body]
}

-- Each loop: each var in expr { ... }
blend parse_each(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "KW", "each")
    preserve var_tok = parser_expect(tokens, pos_ref, "ID", "")
    parser_expect(tokens, pos_ref, "KW", "in")
    preserve iter_expr = parse_expression(tokens, pos_ref)
    preserve body = parse_block(tokens, pos_ref)
    yield ["Each", var_tok[1], iter_expr, body]
}

-- Loop (infinite loop with snap)
blend parse_loop(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "KW", "loop")
    preserve body = parse_block(tokens, pos_ref)
    yield ["Loop", body]
}

-- ============================================================
-- BOWL / MEDLEY / SORT PARSERS
-- ============================================================

blend parse_bowl_def(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "KW", "bowl")
    preserve name = parser_advance(tokens, pos_ref)[1]
    preserve lb = from_char_code(123)
    parser_expect(tokens, pos_ref, "DELIM", lb)
    fresh fields = []
    loop {
        preserve peek = parser_peek(tokens, pos_ref)
        if peek[0] == "DELIM" && peek[1] == from_char_code(125) { snap }
        preserve fname = parser_advance(tokens, pos_ref)[1]
        fresh ftype = ""
        if parser_match_delim(tokens, pos_ref, ":") {
            ftype = parser_advance(tokens, pos_ref)[1]
        }
        fields.push([fname, ftype])
        parser_match_delim(tokens, pos_ref, ",")
    }
    parser_expect(tokens, pos_ref, "DELIM", from_char_code(125))
    yield ["BowlDef", name, fields]
}

blend parse_medley_def(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "KW", "medley")
    preserve name = parser_advance(tokens, pos_ref)[1]
    preserve lb = from_char_code(123)
    parser_expect(tokens, pos_ref, "DELIM", lb)
    fresh variants = []
    loop {
        preserve peek = parser_peek(tokens, pos_ref)
        if peek[0] == "DELIM" && peek[1] == from_char_code(125) { snap }
        preserve vname = parser_advance(tokens, pos_ref)[1]
        fresh vfields = []
        if parser_match_delim(tokens, pos_ref, "(") {
            preserve peek2 = parser_peek(tokens, pos_ref)
            if !(peek2[0] == "DELIM" && peek2[1] == ")") {
                preserve f1 = parser_advance(tokens, pos_ref)[1]
                if parser_match_delim(tokens, pos_ref, ":") {
                    parser_advance(tokens, pos_ref)
                }
                vfields.push(f1)
                loop {
                    if !parser_match_delim(tokens, pos_ref, ",") { snap }
                    preserve fn2 = parser_advance(tokens, pos_ref)[1]
                    if parser_match_delim(tokens, pos_ref, ":") {
                        parser_advance(tokens, pos_ref)
                    }
                    vfields.push(fn2)
                }
            }
            parser_expect(tokens, pos_ref, "DELIM", ")")
        }
        variants.push([vname, vfields])
        parser_match_delim(tokens, pos_ref, ",")
    }
    parser_expect(tokens, pos_ref, "DELIM", from_char_code(125))
    yield ["MedleyDef", name, variants]
}

blend parse_sort_expr(tokens, pos_ref) {
    parser_expect(tokens, pos_ref, "KW", "sort")
    preserve subject = parse_expression(tokens, pos_ref)
    preserve lb = from_char_code(123)
    parser_expect(tokens, pos_ref, "DELIM", lb)
    fresh arms = []
    loop {
        preserve peek = parser_peek(tokens, pos_ref)
        if peek[0] == "DELIM" && peek[1] == from_char_code(125) { snap }
        preserve pattern = parse_sort_pattern(tokens, pos_ref)
        parser_expect(tokens, pos_ref, "OP", "=>")
        fresh body = []
        preserve peek2 = parser_peek(tokens, pos_ref)
        if peek2[0] == "DELIM" && peek2[1] == lb {
            body = parse_block(tokens, pos_ref)
        } else {
            body = ["Block", [parse_expression(tokens, pos_ref)]]
        }
        arms.push([pattern, body])
        parser_match_delim(tokens, pos_ref, ",")
    }
    parser_expect(tokens, pos_ref, "DELIM", from_char_code(125))
    yield ["Sort", subject, arms]
}

blend parse_sort_pattern(tokens, pos_ref) {
    preserve tok = parser_peek(tokens, pos_ref)
    -- Wildcard _
    if tok[0] == "ID" && tok[1] == "_" {
        parser_advance(tokens, pos_ref)
        yield ["PatWild"]
    }
    -- Number literal
    if tok[0] == "NUM" {
        parser_advance(tokens, pos_ref)
        yield ["PatLit", tok[1]]
    }
    -- String literal
    if tok[0] == "STR" {
        parser_advance(tokens, pos_ref)
        yield ["PatLit", tok[1]]
    }
    -- Boolean
    if tok[0] == "KW" && tok[1] == "true" {
        parser_advance(tokens, pos_ref)
        yield ["PatLit", true]
    }
    if tok[0] == "KW" && tok[1] == "false" {
        parser_advance(tokens, pos_ref)
        yield ["PatLit", false]
    }
    -- ripe(binding) / rot(binding) / pit
    if tok[0] == "KW" && tok[1] == "ripe" {
        parser_advance(tokens, pos_ref)
        fresh bindings = []
        if parser_match_delim(tokens, pos_ref, "(") {
            preserve b = parser_advance(tokens, pos_ref)[1]
            bindings.push(b)
            parser_expect(tokens, pos_ref, "DELIM", ")")
        }
        yield ["PatVariant", "Ripe", "ripe", bindings]
    }
    if tok[0] == "KW" && tok[1] == "rot" {
        parser_advance(tokens, pos_ref)
        fresh bindings = []
        if parser_match_delim(tokens, pos_ref, "(") {
            preserve b = parser_advance(tokens, pos_ref)[1]
            bindings.push(b)
            parser_expect(tokens, pos_ref, "DELIM", ")")
        }
        yield ["PatVariant", "Harvest", "rot", bindings]
    }
    if tok[0] == "KW" && tok[1] == "pit" {
        parser_advance(tokens, pos_ref)
        yield ["PatVariant", "Ripe", "pit", []]
    }
    -- Identifier: binding or MedleyName.Variant(bindings)
    if tok[0] == "ID" || tok[0] == "KW" {
        preserve name = parser_advance(tokens, pos_ref)[1]
        if parser_match_delim(tokens, pos_ref, ".") {
            preserve vname = parser_advance(tokens, pos_ref)[1]
            fresh bindings = []
            if parser_match_delim(tokens, pos_ref, "(") {
                preserve peek3 = parser_peek(tokens, pos_ref)
                if !(peek3[0] == "DELIM" && peek3[1] == ")") {
                    bindings.push(parser_advance(tokens, pos_ref)[1])
                    loop {
                        if !parser_match_delim(tokens, pos_ref, ",") { snap }
                        bindings.push(parser_advance(tokens, pos_ref)[1])
                    }
                }
                parser_expect(tokens, pos_ref, "DELIM", ")")
            }
            yield ["PatVariant", name, vname, bindings]
        }
        yield ["PatBind", name]
    }
    -- Negative number
    if tok[0] == "OP" && tok[1] == "-" {
        parser_advance(tokens, pos_ref)
        preserve num = parser_advance(tokens, pos_ref)
        yield ["PatLit", 0 - num[1]]
    }
    parser_advance(tokens, pos_ref)
    yield ["PatWild"]
}

-- ============================================================
-- EXPRESSION PARSER (precedence climbing)
-- ============================================================

blend parse_expression(tokens, pos_ref) {
    parse_or(tokens, pos_ref)
}

blend parse_or(tokens, pos_ref) {
    fresh left = parse_and(tokens, pos_ref)
    loop {
        if !parser_match_op(tokens, pos_ref, "||") {
            snap
        }
        preserve right = parse_and(tokens, pos_ref)
        left = ["BinOp", "||", left, right]
    }
    left
}

blend parse_and(tokens, pos_ref) {
    fresh left = parse_equality(tokens, pos_ref)
    loop {
        if !parser_match_op(tokens, pos_ref, "&&") {
            snap
        }
        preserve right = parse_equality(tokens, pos_ref)
        left = ["BinOp", "&&", left, right]
    }
    left
}

blend parse_equality(tokens, pos_ref) {
    fresh left = parse_comparison(tokens, pos_ref)
    loop {
        preserve tok = parser_peek(tokens, pos_ref)
        if tok[0] == "OP" && (tok[1] == "==" || tok[1] == "!=") {
            preserve op = tok[1]
            parser_advance(tokens, pos_ref)
            preserve right = parse_comparison(tokens, pos_ref)
            left = ["BinOp", op, left, right]
        } else {
            snap
        }
    }
    left
}

blend parse_comparison(tokens, pos_ref) {
    fresh left = parse_pipeline(tokens, pos_ref)
    loop {
        preserve tok = parser_peek(tokens, pos_ref)
        if tok[0] == "OP" && (tok[1] == "<" || tok[1] == ">" || tok[1] == "<=" || tok[1] == ">=") {
            preserve op = tok[1]
            parser_advance(tokens, pos_ref)
            preserve right = parse_pipeline(tokens, pos_ref)
            left = ["BinOp", op, left, right]
        } else {
            snap
        }
    }
    left
}

blend parse_pipeline(tokens, pos_ref) {
    fresh left = parse_range(tokens, pos_ref)
    loop {
        preserve tok = parser_peek(tokens, pos_ref)
        if tok[0] == "OP" && tok[1] == "~>" {
            parser_advance(tokens, pos_ref)
            -- Right side: parse a postfix expression (function call or identifier)
            preserve right = parse_postfix(tokens, pos_ref)
            -- Transform: left ~> f(args) => f(left, args)
            --            left ~> f      => f(left)
            if right[0] == "Call" {
                -- Insert left as first arg
                fresh new_args = [left]
                each i in 0..right[2].len() {
                    new_args.push(right[2][i])
                }
                left = ["Call", right[1], new_args]
            } else {
                -- Just call with left as sole arg
                left = ["Call", right, [left]]
            }
        } else {
            snap
        }
    }
    left
}

blend parse_range(tokens, pos_ref) {
    preserve left = parse_addition(tokens, pos_ref)
    preserve tok = parser_peek(tokens, pos_ref)
    if tok[0] == "OP" && tok[1] == ".." {
        parser_advance(tokens, pos_ref)
        preserve right = parse_addition(tokens, pos_ref)
        yield ["Range", left, right, false]
    }
    if tok[0] == "OP" && tok[1] == "..=" {
        parser_advance(tokens, pos_ref)
        preserve right = parse_addition(tokens, pos_ref)
        yield ["Range", left, right, true]
    }
    left
}

blend parse_addition(tokens, pos_ref) {
    fresh left = parse_multiplication(tokens, pos_ref)
    loop {
        preserve tok = parser_peek(tokens, pos_ref)
        if tok[0] == "OP" && (tok[1] == "+" || tok[1] == "-") {
            preserve op = tok[1]
            parser_advance(tokens, pos_ref)
            preserve right = parse_multiplication(tokens, pos_ref)
            left = ["BinOp", op, left, right]
        } else {
            snap
        }
    }
    left
}

blend parse_multiplication(tokens, pos_ref) {
    fresh left = parse_unary(tokens, pos_ref)
    loop {
        preserve tok = parser_peek(tokens, pos_ref)
        if tok[0] == "OP" && (tok[1] == "*" || tok[1] == "/" || tok[1] == "%") {
            preserve op = tok[1]
            parser_advance(tokens, pos_ref)
            preserve right = parse_unary(tokens, pos_ref)
            left = ["BinOp", op, left, right]
        } else {
            snap
        }
    }
    left
}

blend parse_unary(tokens, pos_ref) {
    preserve tok = parser_peek(tokens, pos_ref)
    if tok[0] == "OP" && tok[1] == "-" {
        parser_advance(tokens, pos_ref)
        preserve operand = parse_unary(tokens, pos_ref)
        yield ["Unary", "-", operand]
    }
    if tok[0] == "OP" && tok[1] == "!" {
        parser_advance(tokens, pos_ref)
        preserve operand = parse_unary(tokens, pos_ref)
        yield ["Unary", "!", operand]
    }
    parse_postfix(tokens, pos_ref)
}

blend parse_postfix(tokens, pos_ref) {
    fresh expr = parse_primary(tokens, pos_ref)
    loop {
        preserve tok = parser_peek(tokens, pos_ref)
        -- field access: .name or .method(args)
        if tok[0] == "DELIM" && tok[1] == "." {
            parser_advance(tokens, pos_ref)
            -- Accept both ID and KW for field/variant names
            preserve field_tok = parser_advance(tokens, pos_ref)
            preserve next2 = parser_peek(tokens, pos_ref)
            if next2[0] == "DELIM" && next2[1] == "(" {
                -- method call
                parser_advance(tokens, pos_ref)
                fresh args = []
                preserve peek3 = parser_peek(tokens, pos_ref)
                if !(peek3[0] == "DELIM" && peek3[1] == ")") {
                    args.push(parse_expression(tokens, pos_ref))
                    loop {
                        if !parser_match_delim(tokens, pos_ref, ",") {
                            snap
                        }
                        args.push(parse_expression(tokens, pos_ref))
                    }
                }
                parser_expect(tokens, pos_ref, "DELIM", ")")
                expr = ["MethodCall", expr, field_tok[1], args]
            } else {
                expr = ["Field", expr, field_tok[1]]
            }
            skip
        }
        -- index access: [expr]
        if tok[0] == "DELIM" && tok[1] == "[" {
            parser_advance(tokens, pos_ref)
            preserve idx = parse_expression(tokens, pos_ref)
            parser_expect(tokens, pos_ref, "DELIM", "]")
            expr = ["Index", expr, idx]
            skip
        }
        -- bowl literal: Name { field: value, ... }
        preserve lb = from_char_code(123)
        if tok[0] == "DELIM" && tok[1] == lb && expr[0] == "Ident" {
            -- Lookahead: is this really a bowl literal (ID : pattern)?
            preserve saved_pos = pos_ref[0]
            parser_advance(tokens, pos_ref)
            preserve peek_b = parser_peek(tokens, pos_ref)
            preserve peek_b2_pos = pos_ref[0] + 1
            fresh is_bowl = false
            if (peek_b[0] == "ID" || peek_b[0] == "KW") && peek_b2_pos < tokens.len() && tokens[peek_b2_pos][0] == "DELIM" && tokens[peek_b2_pos][1] == ":" {
                is_bowl = true
            }
            if is_bowl {
                fresh fields = []
                loop {
                    preserve fp = parser_peek(tokens, pos_ref)
                    if fp[0] == "DELIM" && fp[1] == from_char_code(125) { snap }
                    preserve fname = parser_advance(tokens, pos_ref)[1]
                    parser_expect(tokens, pos_ref, "DELIM", ":")
                    preserve fval = parse_expression(tokens, pos_ref)
                    fields.push([fname, fval])
                    parser_match_delim(tokens, pos_ref, ",")
                }
                parser_expect(tokens, pos_ref, "DELIM", from_char_code(125))
                expr = ["BowlLit", expr[1], fields]
                skip
            } else {
                -- Not a bowl literal, restore position
                pos_ref[0] = saved_pos
            }
        }
        -- function call: (args)
        if tok[0] == "DELIM" && tok[1] == "(" {
            parser_advance(tokens, pos_ref)
            fresh args = []
            preserve peek4 = parser_peek(tokens, pos_ref)
            if !(peek4[0] == "DELIM" && peek4[1] == ")") {
                args.push(parse_expression(tokens, pos_ref))
                loop {
                    if !parser_match_delim(tokens, pos_ref, ",") {
                        snap
                    }
                    args.push(parse_expression(tokens, pos_ref))
                }
            }
            parser_expect(tokens, pos_ref, "DELIM", ")")
            expr = ["Call", expr, args]
            skip
        }
        snap
    }
    expr
}

blend parse_primary(tokens, pos_ref) {
    preserve tok = parser_peek(tokens, pos_ref)

    -- number
    if tok[0] == "NUM" {
        parser_advance(tokens, pos_ref)
        yield ["NumLit", tok[1]]
    }

    -- string
    if tok[0] == "STR" {
        parser_advance(tokens, pos_ref)
        yield ["StrLit", tok[1]]
    }

    -- interpolated string
    if tok[0] == "INTERP" {
        parser_advance(tokens, pos_ref)
        yield ["InterpStr", tok[1]]
    }

    -- boolean
    if tok[0] == "KW" && tok[1] == "true" {
        parser_advance(tokens, pos_ref)
        yield ["BoolLit", true]
    }
    if tok[0] == "KW" && tok[1] == "false" {
        parser_advance(tokens, pos_ref)
        yield ["BoolLit", false]
    }

    -- parenthesized expression
    if tok[0] == "DELIM" && tok[1] == "(" {
        parser_advance(tokens, pos_ref)
        preserve expr = parse_expression(tokens, pos_ref)
        parser_expect(tokens, pos_ref, "DELIM", ")")
        yield expr
    }

    -- basket literal [...]
    if tok[0] == "DELIM" && tok[1] == "[" {
        parser_advance(tokens, pos_ref)
        fresh items = []
        preserve peek5 = parser_peek(tokens, pos_ref)
        if !(peek5[0] == "DELIM" && peek5[1] == "]") {
            items.push(parse_expression(tokens, pos_ref))
            loop {
                if !parser_match_delim(tokens, pos_ref, ",") {
                    snap
                }
                items.push(parse_expression(tokens, pos_ref))
            }
        }
        parser_expect(tokens, pos_ref, "DELIM", "]")
        yield ["BasketLit", items]
    }

    -- closure |params| { body }
    if tok[0] == "DELIM" && tok[1] == "|" {
        parser_advance(tokens, pos_ref)
        fresh params = []
        preserve peek6 = parser_peek(tokens, pos_ref)
        if !(peek6[0] == "DELIM" && peek6[1] == "|") {
            preserve p1 = parser_expect(tokens, pos_ref, "ID", "")
            params.push(p1[1])
            loop {
                if !parser_match_delim(tokens, pos_ref, ",") {
                    snap
                }
                preserve pn = parser_expect(tokens, pos_ref, "ID", "")
                params.push(pn[1])
            }
        }
        parser_expect(tokens, pos_ref, "DELIM", "|")
        preserve body = parse_block(tokens, pos_ref)
        yield ["Closure", params, body]
    }

    -- sort expression
    if tok[0] == "KW" && tok[1] == "sort" {
        yield parse_sort_expr(tokens, pos_ref)
    }

    -- if expression
    if tok[0] == "KW" && tok[1] == "if" {
        yield parse_if(tokens, pos_ref)
    }

    -- ripe(value)
    if tok[0] == "KW" && tok[1] == "ripe" {
        parser_advance(tokens, pos_ref)
        parser_expect(tokens, pos_ref, "DELIM", "(")
        preserve val = parse_expression(tokens, pos_ref)
        parser_expect(tokens, pos_ref, "DELIM", ")")
        yield ["Ripe", val]
    }

    -- rot(message)
    if tok[0] == "KW" && tok[1] == "rot" {
        parser_advance(tokens, pos_ref)
        parser_expect(tokens, pos_ref, "DELIM", "(")
        preserve val = parse_expression(tokens, pos_ref)
        parser_expect(tokens, pos_ref, "DELIM", ")")
        yield ["Rot", val]
    }

    -- pit
    if tok[0] == "KW" && tok[1] == "pit" {
        parser_advance(tokens, pos_ref)
        yield ["Pit"]
    }

    -- identifier (or builtin-as-identifier like display used as expression)
    if tok[0] == "ID" {
        parser_advance(tokens, pos_ref)
        yield ["Ident", tok[1]]
    }

    -- builtins/keywords used as identifiers in expression position
    if tok[0] == "KW" {
        if tok[1] == "display" || tok[1] == "peel" || tok[1] == "abs" ||
           tok[1] == "min" || tok[1] == "max" || tok[1] == "to_apple" ||
           tok[1] == "to_date" || tok[1] == "to_banana" || tok[1] == "char_code" ||
           tok[1] == "from_char_code" || tok[1] == "sqrt" || tok[1] == "read_file" ||
           tok[1] == "read_stdin" || tok[1] == "args" {
            parser_advance(tokens, pos_ref)
            yield ["Ident", tok[1]]
        }
    }

    display("Parse error: unexpected token " + tok[0] + " '" + to_banana(tok[1]) + "' at line " + to_banana(tok[2]))
    yield ["Ident", "ERROR"]
}

-- ============================================================
-- CODE GENERATOR (Python output)
-- ============================================================

blend make_indent(level) {
    fresh result = ""
    fresh i = 0
    while i < level {
        result = result + "    "
        i = i + 1
    }
    result
}

blend py_escape_str(s) {
    fresh result = ""
    fresh i = 0
    while i < s.len() {
        fresh c = s[i]
        if c == "\\" {
            result = result + "\\\\"
        } else if c == "\"" {
            result = result + "\\\""
        } else if c == "\n" {
            result = result + "\\n"
        } else if c == "\t" {
            result = result + "\\t"
        } else {
            result = result + c
        }
        i = i + 1
    }
    result
}

blend safe_name(name) {
    -- Rename Python reserved words
    if name == "from" { yield "from_" }
    if name == "in" { yield "in_" }
    if name == "is" { yield "is_" }
    if name == "as" { yield "as_" }
    if name == "class" { yield "class_" }
    if name == "def" { yield "def_" }
    if name == "import" { yield "import_" }
    if name == "lambda" { yield "lambda_" }
    if name == "pass" { yield "pass_" }
    if name == "with" { yield "with_" }
    if name == "type" { yield "type_" }
    if name == "None" { yield "None_" }
    if name == "True" { yield "True_" }
    if name == "False" { yield "False_" }
    name
}

blend generate(node, level) {
    preserve typ = node[0]

    if typ == "Program" {
        yield gen_program(node, level)
    }
    if typ == "BlendDef" {
        yield gen_blend_def(node, level)
    }
    if typ == "Block" {
        yield gen_block(node, level)
    }
    if typ == "VarDef" {
        yield gen_var_def(node, level)
    }
    if typ == "Assign" {
        yield gen_assign(node, level)
    }
    if typ == "If" {
        yield gen_if(node, level)
    }
    if typ == "While" {
        yield gen_while(node, level)
    }
    if typ == "Each" {
        yield gen_each(node, level)
    }
    if typ == "Loop" {
        yield gen_loop(node, level)
    }
    if typ == "Snap" {
        yield make_indent(level) + "break"
    }
    if typ == "Skip" {
        yield make_indent(level) + "continue"
    }
    if typ == "Yield" {
        yield make_indent(level) + "return " + gen_expr(node[1])
    }
    if typ == "Display" {
        yield gen_display(node, level)
    }
    if typ == "BowlDef" {
        yield gen_bowl_def(node, level)
    }
    if typ == "MedleyDef" {
        yield gen_medley_def(node, level)
    }
    if typ == "Sort" {
        yield gen_sort(node, level)
    }
    if typ == "Toss" {
        yield make_indent(level) + "raise Exception(_fs_fmt(" + gen_expr(node[1]) + "))"
    }
    -- expression statement
    yield make_indent(level) + gen_expr(node)
}

blend gen_program(node, level) {
    -- Emit runtime helpers for FS-compatible output and integer division
    fresh result = "def _fs_div(a, b):\n"
    result = result + "    if isinstance(a, int) and isinstance(b, int): return a // b\n"
    result = result + "    return a / b\n"
    result = result + "def _fs_fmt(v):\n"
    result = result + "    if isinstance(v, bool): return 'true' if v else 'false'\n"
    result = result + "    if isinstance(v, list): return '[' + ', '.join(_fs_fmt(x) for x in v) + ']'\n"
    result = result + "    if isinstance(v, _FsVariant): return repr(v)\n"
    result = result + "    if isinstance(v, _FsBowl): return repr(v)\n"
    result = result + "    if v is None: return 'pit'\n"
    result = result + "    if isinstance(v, float): return f'" + from_char_code(123) + "v:.10g" + from_char_code(125) + "'\n"
    result = result + "    return str(v)\n"
    result = result + "def _fs_display(*args): print(' '.join(_fs_fmt(a) for a in args))\n"
    result = result + "_fs_type_map = " + from_char_code(123) + "'int':'Apple','float':'Date','str':'Banana','bool':'Cherry','list':'Basket','NoneType':'Pit'" + from_char_code(125) + "\n"
    result = result + "def _fs_peel(v): return _fs_type_map.get(type(v).__name__, type(v).__name__)\n"
    result = result + "def char_code(c): return ord(c)\n"
    result = result + "def from_char_code(n): return chr(n)\n"
    -- Bowl: simple class with named fields
    result = result + "class _FsBowl:\n"
    result = result + "    def __init__(self, _name, **fields):\n"
    result = result + "        self._name = _name\n"
    result = result + "        self.__dict__.update(fields)\n"
    result = result + "    def __repr__(self):\n"
    result = result + "        fields = ', '.join(f'" + from_char_code(123) + "k" + from_char_code(125) + ": " + from_char_code(123) + "_fs_fmt(v)" + from_char_code(125) + "' for k, v in self.__dict__.items() if k != '_name')\n"
    result = result + "        return f'" + from_char_code(123) + "self._name" + from_char_code(125) + " " + from_char_code(123) + from_char_code(123) + " " + from_char_code(123) + "fields" + from_char_code(125) + " " + from_char_code(125) + from_char_code(125) + "'\n"
    -- Medley: tagged tuple
    result = result + "class _FsVariant:\n"
    result = result + "    def __init__(self, medley, variant, *args):\n"
    result = result + "        self.medley = medley; self.variant = variant; self.values = list(args)\n"
    result = result + "    def __repr__(self):\n"
    result = result + "        if self.values: return f'" + from_char_code(123) + "self.medley" + from_char_code(125) + "." + from_char_code(123) + "self.variant" + from_char_code(125) + "(" + from_char_code(123) + "', '.join(_fs_fmt(v) for v in self.values)" + from_char_code(125) + ")'\n"
    result = result + "        return f'" + from_char_code(123) + "self.medley" + from_char_code(125) + "." + from_char_code(123) + "self.variant" + from_char_code(125) + "'\n"
    result = result + "    def __eq__(self, other): return isinstance(other, _FsVariant) and self.medley == other.medley and self.variant == other.variant and self.values == other.values\n\n"

    preserve funcs = node[1]
    preserve stmts = node[2]

    -- generate function definitions
    each i in 0..funcs.len() {
        result = result + generate(funcs[i], level) + "\n"
    }

    -- generate top-level statements
    each i in 0..stmts.len() {
        result = result + generate(stmts[i], level) + "\n"
    }

    -- if there's a main function, call it
    each i in 0..funcs.len() {
        if funcs[i][1] == "main" {
            result = result + "main()\n"
        }
    }

    result
}

blend gen_blend_def(node, level) {
    preserve name = node[1]
    preserve params = node[2]
    preserve body = node[3]

    fresh param_str = ""
    each i in 0..params.len() {
        if i > 0 {
            param_str = param_str + ", "
        }
        param_str = param_str + safe_name(params[i])
    }

    fresh result = make_indent(level) + "def " + name + "(" + param_str + "):\n"
    preserve stmts = body[1]
    if stmts.len() == 0 {
        result = result + make_indent(level + 1) + "pass"
        yield result
    }
    -- Generate all statements, adding 'return' before the last one
    each i in 0..stmts.len() {
        preserve stmt = stmts[i]
        preserve is_last = (i == stmts.len() - 1)
        if is_last {
            result = result + gen_with_return(stmt, level + 1) + "\n"
        } else {
            result = result + generate(stmt, level + 1) + "\n"
        }
    }
    result
}

blend is_expression_node(node) {
    preserve typ = node[0]
    if typ == "BinOp" || typ == "UnaryOp" || typ == "Call" || typ == "MethodCall" ||
       typ == "Ident" || typ == "NumLit" || typ == "FloatLit" || typ == "StrLit" ||
       typ == "BoolLit" || typ == "BasketLit" || typ == "Index" || typ == "Field" ||
       typ == "StrInterp" || typ == "Closure" || typ == "InterpStr" ||
       typ == "Ripe" || typ == "Rot" || typ == "Pit" || typ == "BowlLit" {
        yield true
    }
    false
}

blend gen_with_return(node, level) {
    -- Generate a statement that should be the return value of a function
    -- If it's a bare expression, add 'return'. If it's an if/else, recurse into branches.
    preserve typ = node[0]
    -- Don't add return to method calls that are pure side-effects (swap, push)
    -- But DO add return to pop() since it returns a value
    if typ == "MethodCall" {
        if node[2] == "swap" || node[2] == "push" || node[2] == "append" {
            yield generate(node, level)
        }
    }
    if is_expression_node(node) {
        yield make_indent(level) + "return " + gen_expr(node)
    }
    if typ == "If" {
        -- Generate if with returns in each branch
        fresh code = make_indent(level) + "if " + gen_expr(node[1]) + ":\n"
        preserve then_stmts = node[2][1]
        if then_stmts.len() > 0 {
            each i in 0..(then_stmts.len() - 1) {
                code = code + generate(then_stmts[i], level + 1) + "\n"
            }
            code = code + gen_with_return(then_stmts[then_stmts.len() - 1], level + 1) + "\n"
        } else {
            code = code + make_indent(level + 1) + "pass\n"
        }
        preserve else_stmts = node[3][1]
        if else_stmts.len() > 0 {
            -- Check for else-if chain
            if else_stmts.len() == 1 && else_stmts[0][0] == "If" {
                code = code + make_indent(level) + "el" + gen_with_return(else_stmts[0], level).trim() + "\n"
            } else {
                code = code + make_indent(level) + "else:\n"
                each i in 0..(else_stmts.len() - 1) {
                    code = code + generate(else_stmts[i], level + 1) + "\n"
                }
                code = code + gen_with_return(else_stmts[else_stmts.len() - 1], level + 1) + "\n"
            }
        }
        yield code
    }
    if typ == "Sort" {
        -- Generate sort with return in each arm
        preserve subject = node[1]
        preserve arms = node[2]
        preserve subj_var = "_sort_subj"
        fresh code = make_indent(level) + subj_var + " = " + gen_expr(subject) + "\n"
        fresh first_arm = true
        each i in 0..arms.len() {
            preserve pattern = arms[i][0]
            preserve body = arms[i][1]
            preserve pat_type = pattern[0]
            if pat_type == "PatWild" || pat_type == "PatBind" {
                if first_arm {
                    code = code + make_indent(level) + "if True:\n"
                } else {
                    code = code + make_indent(level) + "else:\n"
                }
                if pat_type == "PatBind" {
                    code = code + make_indent(level + 1) + safe_name(pattern[1]) + " = " + subj_var + "\n"
                }
                preserve stmts = body[1]
                if stmts.len() > 0 {
                    each j in 0..(stmts.len() - 1) {
                        code = code + generate(stmts[j], level + 1) + "\n"
                    }
                    code = code + gen_with_return(stmts[stmts.len() - 1], level + 1) + "\n"
                } else {
                    code = code + make_indent(level + 1) + "pass\n"
                }
            } else if pat_type == "PatLit" {
                fresh kw = "elif "
                if first_arm { kw = "if " }
                code = code + make_indent(level) + kw + subj_var + " == " + gen_sort_lit(pattern[1]) + ":\n"
                preserve stmts = body[1]
                if stmts.len() > 0 {
                    each j in 0..(stmts.len() - 1) {
                        code = code + generate(stmts[j], level + 1) + "\n"
                    }
                    code = code + gen_with_return(stmts[stmts.len() - 1], level + 1) + "\n"
                } else {
                    code = code + make_indent(level + 1) + "pass\n"
                }
            } else if pat_type == "PatVariant" {
                fresh kw = "elif "
                if first_arm { kw = "if " }
                code = code + make_indent(level) + kw + "isinstance(" + subj_var + ", _FsVariant) and " + subj_var + ".variant == \"" + pattern[2] + "\":\n"
                each j in 0..pattern[3].len() {
                    code = code + make_indent(level + 1) + safe_name(pattern[3][j]) + " = " + subj_var + ".values[" + to_banana(j) + "]\n"
                }
                preserve stmts = body[1]
                if stmts.len() > 0 {
                    each j in 0..(stmts.len() - 1) {
                        code = code + generate(stmts[j], level + 1) + "\n"
                    }
                    code = code + gen_with_return(stmts[stmts.len() - 1], level + 1) + "\n"
                } else if pattern[3].len() == 0 {
                    code = code + make_indent(level + 1) + "pass\n"
                }
            }
            first_arm = false
        }
        yield code
    }
    -- For everything else (assignments, loops, etc.) just generate normally
    generate(node, level)
}

blend gen_block_stmts(node, level) {
    preserve stmts = node[1]
    fresh result = ""
    each i in 0..stmts.len() {
        preserve line = generate(stmts[i], level)
        result = result + line + "\n"
    }
    result
}

blend gen_block(node, level) {
    gen_block_stmts(node, level)
}

blend gen_var_def(node, level) {
    preserve name = safe_name(node[2])
    preserve value = node[3]
    make_indent(level) + name + " = " + gen_expr(value)
}

blend gen_assign(node, level) {
    preserve target = node[1]
    preserve value = node[2]
    if target[0] == "Ident" {
        yield make_indent(level) + safe_name(target[1]) + " = " + gen_expr(value)
    }
    if target[0] == "Index" {
        yield make_indent(level) + gen_expr(target[1]) + "[" + gen_expr(target[2]) + "] = " + gen_expr(value)
    }
    if target[0] == "Field" {
        yield make_indent(level) + gen_expr(target[1]) + "." + target[2] + " = " + gen_expr(value)
    }
    make_indent(level) + gen_expr(target) + " = " + gen_expr(value)
}

blend gen_if(node, level) {
    preserve cond = node[1]
    preserve then_block = node[2]
    preserve else_block = node[3]

    fresh result = make_indent(level) + "if " + gen_expr(cond) + ":\n"
    preserve then_code = gen_block_stmts(then_block, level + 1)
    if then_code == "" {
        result = result + make_indent(level + 1) + "pass\n"
    } else {
        result = result + then_code
    }

    preserve else_stmts = else_block[1]
    if else_stmts.len() > 0 {
        -- check if it's an else-if chain
        if else_stmts.len() == 1 && else_stmts[0][0] == "If" {
            preserve elif_node = else_stmts[0]
            result = result + make_indent(level) + "el" + gen_if_inner(elif_node, level)
        } else {
            result = result + make_indent(level) + "else:\n"
            preserve else_code = gen_block_stmts(else_block, level + 1)
            result = result + else_code
        }
    }
    result
}

blend gen_if_inner(node, level) {
    preserve cond = node[1]
    preserve then_block = node[2]
    preserve else_block = node[3]

    fresh result = "if " + gen_expr(cond) + ":\n"
    preserve then_code = gen_block_stmts(then_block, level + 1)
    if then_code == "" {
        result = result + make_indent(level + 1) + "pass\n"
    } else {
        result = result + then_code
    }

    preserve else_stmts = else_block[1]
    if else_stmts.len() > 0 {
        if else_stmts.len() == 1 && else_stmts[0][0] == "If" {
            preserve elif_node = else_stmts[0]
            result = result + make_indent(level) + "el" + gen_if_inner(elif_node, level)
        } else {
            result = result + make_indent(level) + "else:\n"
            preserve else_code = gen_block_stmts(else_block, level + 1)
            result = result + else_code
        }
    }
    result
}

blend gen_while(node, level) {
    preserve cond = node[1]
    preserve body = node[2]
    fresh result = make_indent(level) + "while " + gen_expr(cond) + ":\n"
    preserve body_code = gen_block_stmts(body, level + 1)
    if body_code == "" {
        result = result + make_indent(level + 1) + "pass\n"
    } else {
        result = result + body_code
    }
    result
}

blend gen_each(node, level) {
    preserve var_name = safe_name(node[1])
    preserve iter_expr = node[2]
    preserve body = node[3]

    fresh result = ""
    if iter_expr[0] == "Range" {
        preserve start_expr = gen_expr(iter_expr[1])
        preserve end_expr = gen_expr(iter_expr[2])
        preserve inclusive = iter_expr[3]
        if inclusive {
            result = make_indent(level) + "for " + var_name + " in range(" + start_expr + ", " + end_expr + " + 1):\n"
        } else {
            result = make_indent(level) + "for " + var_name + " in range(" + start_expr + ", " + end_expr + "):\n"
        }
    } else {
        result = make_indent(level) + "for " + var_name + " in " + gen_expr(iter_expr) + ":\n"
    }
    preserve body_code = gen_block_stmts(body, level + 1)
    if body_code == "" {
        result = result + make_indent(level + 1) + "pass\n"
    } else {
        result = result + body_code
    }
    result
}

blend gen_loop(node, level) {
    preserve body = node[1]
    fresh result = make_indent(level) + "while True:\n"
    preserve body_code = gen_block_stmts(body, level + 1)
    if body_code == "" {
        result = result + make_indent(level + 1) + "pass\n"
    } else {
        result = result + body_code
    }
    result
}

blend gen_display(node, level) {
    preserve args = node[1]
    fresh arg_strs = ""
    each i in 0..args.len() {
        if i > 0 {
            arg_strs = arg_strs + ", "
        }
        arg_strs = arg_strs + gen_expr(args[i])
    }
    make_indent(level) + "_fs_display(" + arg_strs + ")"
}

blend gen_bowl_def(node, level) {
    -- Bowl becomes a factory function: def BowlName(**kw): return _FsBowl("Name", **kw)
    preserve name = node[1]
    make_indent(level) + "def " + name + "(**kw): return _FsBowl(\"" + name + "\", **kw)"
}

blend gen_medley_def(node, level) {
    -- Medley becomes a namespace class with variant constructors
    preserve name = node[1]
    preserve variants = node[2]
    fresh result = make_indent(level) + "class " + name + ":\n"
    each i in 0..variants.len() {
        preserve vname = variants[i][0]
        preserve safe_vname = safe_name(vname)
        preserve vfields = variants[i][1]
        if vfields.len() == 0 {
            result = result + make_indent(level + 1) + safe_vname + " = _FsVariant(\"" + name + "\", \"" + vname + "\")\n"
        } else {
            fresh pstr = ""
            each j in 0..vfields.len() {
                if j > 0 { pstr = pstr + ", " }
                pstr = pstr + safe_name(vfields[j])
            }
            result = result + make_indent(level + 1) + "@staticmethod\n"
            result = result + make_indent(level + 1) + "def " + safe_vname + "(" + pstr + "): return _FsVariant(\"" + name + "\", \"" + vname + "\", " + pstr + ")\n"
        }
    }
    result
}

blend gen_sort(node, level) {
    -- Sort becomes a chain of if/elif checking patterns
    preserve subject = node[1]
    preserve arms = node[2]
    preserve subj_var = "_sort_subj"
    fresh result = make_indent(level) + subj_var + " = " + gen_expr(subject) + "\n"

    fresh first = true
    each i in 0..arms.len() {
        preserve pattern = arms[i][0]
        preserve body = arms[i][1]
        preserve pat_type = pattern[0]

        if pat_type == "PatWild" {
            if first {
                result = result + make_indent(level) + "if True:\n"
            } else {
                result = result + make_indent(level) + "else:\n"
            }
            result = result + gen_sort_body(body, level + 1)
        } else if pat_type == "PatLit" {
            fresh kw = "elif "
            if first { kw = "if " }
            result = result + make_indent(level) + kw + subj_var + " == " + gen_sort_lit(pattern[1]) + ":\n"
            result = result + gen_sort_body(body, level + 1)
        } else if pat_type == "PatBind" {
            preserve bind_name = safe_name(pattern[1])
            if first {
                result = result + make_indent(level) + bind_name + " = " + subj_var + "\n"
                result = result + make_indent(level) + "if True:\n"
            } else {
                result = result + make_indent(level) + "else:\n"
                result = result + make_indent(level + 1) + bind_name + " = " + subj_var + "\n"
            }
            result = result + gen_sort_body(body, level + 1)
        } else if pat_type == "PatVariant" {
            preserve med_name = pattern[1]
            preserve var_name = pattern[2]
            preserve bindings = pattern[3]
            fresh kw = "elif "
            if first { kw = "if " }
            result = result + make_indent(level) + kw + "isinstance(" + subj_var + ", _FsVariant) and " + subj_var + ".variant == \"" + var_name + "\":\n"
            -- Bind captured values
            each j in 0..bindings.len() {
                result = result + make_indent(level + 1) + safe_name(bindings[j]) + " = " + subj_var + ".values[" + to_banana(j) + "]\n"
            }
            result = result + gen_sort_body(body, level + 1)
        }
        first = false
    }
    result
}

blend gen_sort_lit(val) {
    if peel(val) == "Banana" {
        yield "\"" + val + "\""
    }
    if peel(val) == "Cherry" {
        if val { yield "True" }
        yield "False"
    }
    to_banana(val)
}

blend gen_sort_body(body, level) {
    preserve stmts = body[1]
    fresh result = ""
    if stmts.len() == 0 {
        yield make_indent(level) + "pass\n"
    }
    each i in 0..stmts.len() {
        result = result + generate(stmts[i], level) + "\n"
    }
    result
}

-- ============================================================
-- EXPRESSION CODE GENERATOR
-- ============================================================

blend gen_expr(node) {
    preserve typ = node[0]

    if typ == "NumLit" {
        yield to_banana(node[1])
    }
    if typ == "StrLit" {
        yield "\"" + py_escape_str(node[1]) + "\""
    }
    if typ == "InterpStr" {
        yield gen_interp_str(node[1])
    }
    if typ == "BoolLit" {
        if node[1] {
            yield "True"
        } else {
            yield "False"
        }
    }
    if typ == "Ident" {
        yield safe_name(node[1])
    }
    if typ == "BinOp" {
        yield gen_binop(node)
    }
    if typ == "Unary" {
        yield gen_unary(node)
    }
    if typ == "Call" {
        yield gen_call(node)
    }
    if typ == "MethodCall" {
        yield gen_method_call(node)
    }
    if typ == "Index" {
        yield gen_expr(node[1]) + "[" + gen_expr(node[2]) + "]"
    }
    if typ == "Field" {
        yield gen_expr(node[1]) + "." + safe_name(node[2])
    }
    if typ == "BasketLit" {
        yield gen_basket_lit(node)
    }
    if typ == "Range" {
        preserve start_e = gen_expr(node[1])
        preserve end_e = gen_expr(node[2])
        if node[3] {
            yield "range(" + start_e + ", " + end_e + " + 1)"
        } else {
            yield "range(" + start_e + ", " + end_e + ")"
        }
    }
    if typ == "Closure" {
        yield gen_closure(node)
    }
    if typ == "BowlLit" {
        -- BowlLit: ["BowlLit", "Name", [[fname, value_expr], ...]]
        preserve bname = node[1]
        fresh kw_args = ""
        each i in 0..node[2].len() {
            if i > 0 { kw_args = kw_args + ", " }
            kw_args = kw_args + node[2][i][0] + "=" + gen_expr(node[2][i][1])
        }
        yield bname + "(" + kw_args + ")"
    }
    if typ == "Ripe" {
        yield "_FsVariant(\"Ripe\", \"ripe\", " + gen_expr(node[1]) + ")"
    }
    if typ == "Rot" {
        yield "_FsVariant(\"Harvest\", \"rot\", " + gen_expr(node[1]) + ")"
    }
    if typ == "Pit" {
        yield "_FsVariant(\"Ripe\", \"pit\")"
    }
    -- fallback
    "None"
}

blend gen_binop(node) {
    preserve op = node[1]
    preserve left = gen_expr(node[2])
    preserve right = gen_expr(node[3])
    if op == "/" {
        yield "_fs_div(" + left + ", " + right + ")"
    }
    fresh py_op = op
    if op == "&&" {
        py_op = "and"
    }
    if op == "||" {
        py_op = "or"
    }
    "(" + left + " " + py_op + " " + right + ")"
}

blend gen_unary(node) {
    preserve op = node[1]
    preserve operand = gen_expr(node[2])
    if op == "!" {
        yield "(not " + operand + ")"
    }
    "(-" + operand + ")"
}

blend gen_call(node) {
    preserve callee = node[1]
    preserve args = node[2]

    fresh func_name = gen_expr(callee)

    -- map FS builtins to Python
    if func_name == "display" {
        fresh arg_strs = ""
        each i in 0..args.len() {
            if i > 0 {
                arg_strs = arg_strs + ", "
            }
            arg_strs = arg_strs + gen_expr(args[i])
        }
        yield "_fs_display(" + arg_strs + ")"
    }
    if func_name == "to_banana" {
        yield "str(" + gen_expr(args[0]) + ")"
    }
    if func_name == "to_apple" {
        yield "int(" + gen_expr(args[0]) + ")"
    }
    if func_name == "to_date" {
        yield "float(" + gen_expr(args[0]) + ")"
    }
    if func_name == "peel" {
        yield "_fs_peel(" + gen_expr(args[0]) + ")"
    }
    if func_name == "read_file" {
        yield "open(" + gen_expr(args[0]) + ").read()"
    }
    if func_name == "sqrt" {
        yield "__import__('math').sqrt(" + gen_expr(args[0]) + ")"
    }
    -- char_code and from_char_code are NOT mapped to ord/chr here
    -- because user programs may define their own char_code function
    -- (e.g. anagram.fs maps a-z to 0-25). Instead these are defined
    -- as Python helpers in the runtime preamble.
    if func_name == "abs" {
        yield "abs(" + gen_expr(args[0]) + ")"
    }
    if func_name == "min" {
        yield "min(" + gen_expr(args[0]) + ", " + gen_expr(args[1]) + ")"
    }
    if func_name == "max" {
        yield "max(" + gen_expr(args[0]) + ", " + gen_expr(args[1]) + ")"
    }

    fresh arg_strs = ""
    each i in 0..args.len() {
        if i > 0 {
            arg_strs = arg_strs + ", "
        }
        arg_strs = arg_strs + gen_expr(args[i])
    }
    func_name + "(" + arg_strs + ")"
}

blend gen_method_call(node) {
    preserve obj = gen_expr(node[1])
    preserve method = node[2]
    preserve args = node[3]

    -- basket/string methods
    if method == "len" {
        yield "len(" + obj + ")"
    }
    if method == "push" {
        yield obj + ".append(" + gen_expr(args[0]) + ")"
    }
    if method == "pop" {
        yield obj + ".pop()"
    }
    if method == "swap" {
        preserve i_expr = gen_expr(args[0])
        preserve j_expr = gen_expr(args[1])
        yield obj + "[" + i_expr + "], " + obj + "[" + j_expr + "] = " + obj + "[" + j_expr + "], " + obj + "[" + i_expr + "]"
    }
    if method == "copy" {
        yield obj + "[:]"
    }
    if method == "split" {
        yield obj + ".split(" + gen_expr(args[0]) + ")"
    }
    if method == "trim" {
        yield obj + ".strip()"
    }
    if method == "starts_with" {
        yield obj + ".startswith(" + gen_expr(args[0]) + ")"
    }
    if method == "ends_with" {
        yield obj + ".endswith(" + gen_expr(args[0]) + ")"
    }
    if method == "to_lower" {
        yield obj + ".lower()"
    }
    if method == "to_upper" {
        yield obj + ".upper()"
    }
    if method == "replace" {
        yield obj + ".replace(" + gen_expr(args[0]) + ", " + gen_expr(args[1]) + ")"
    }
    if method == "chars" {
        yield "list(" + obj + ")"
    }
    if method == "contains" {
        yield "(" + gen_expr(args[0]) + " in " + obj + ")"
    }

    -- generic method call fallback
    fresh arg_strs = ""
    each i in 0..args.len() {
        if i > 0 {
            arg_strs = arg_strs + ", "
        }
        arg_strs = arg_strs + gen_expr(args[i])
    }
    obj + "." + method + "(" + arg_strs + ")"
}

blend gen_basket_lit(node) {
    preserve items = node[1]
    fresh result = "["
    each i in 0..items.len() {
        if i > 0 {
            result = result + ", "
        }
        result = result + gen_expr(items[i])
    }
    result + "]"
}

blend gen_closure(node) {
    preserve params = node[1]
    preserve body = node[2]
    -- simple single-expression closures become lambdas
    preserve stmts = body[1]
    if stmts.len() == 1 {
        fresh param_str = ""
        each i in 0..params.len() {
            if i > 0 {
                param_str = param_str + ", "
            }
            param_str = param_str + safe_name(params[i])
        }
        preserve expr = stmts[0]
        yield "(lambda " + param_str + ": " + gen_expr(expr) + ")"
    }
    -- multi-statement closures: not trivially supported as lambdas
    -- For now, generate a lambda with the last expression
    fresh param_str = ""
    each i in 0..params.len() {
        if i > 0 {
            param_str = param_str + ", "
        }
        param_str = param_str + safe_name(params[i])
    }
    "(lambda " + param_str + ": None)"
}

blend gen_interp_str(segments) {
    fresh result = "f\""
    each i in 0..segments.len() {
        preserve seg = segments[i]
        if seg[0] == "STR_SEG" {
            result = result + py_escape_str(seg[1])
        } else {
            -- EXPR_SEG: tokenize and parse the sub-expression, then generate
            preserve sub_tokens = tokenize(seg[1])
            fresh sub_pos = [0]
            preserve sub_expr = parse_expression(sub_tokens, sub_pos)
            preserve lbr = from_char_code(123)
            preserve rbr = from_char_code(125)
            result = result + lbr + gen_expr(sub_expr) + rbr
        }
    }
    result + "\""
}

-- ============================================================
-- MAIN: compile a test program
-- ============================================================

blend trim_output(code) {
    -- Remove trailing blank lines, keep structure
    fresh output = ""
    preserve lines = code.split("\n")
    fresh first = true
    each i in 0..lines.len() {
        if lines[i].trim().len() > 0 || !first {
            if !first {
                output = output + "\n"
            }
            output = output + lines[i]
            first = false
        }
    }
    -- Remove trailing newlines
    while output.len() > 0 && output[output.len() - 1] == "\n" {
        fresh new_out = ""
        each i in 0..(output.len() - 1) {
            new_out = new_out + output[i]
        }
        output = new_out
    }
    output
}

blend compile(source) {
    preserve tokens = tokenize(source)
    preserve ast = parse(tokens)
    generate(ast, 0)
}

blend main() {
    preserve cli_args = args()

    fresh source = ""
    if cli_args.len() > 0 {
        -- Read source from file
        source = read_file(cli_args[0])
    } else {
        -- Default demo: compile factorial
        preserve lb = from_char_code(123)
        preserve rb = from_char_code(125)
        preserve nl = from_char_code(10)
        preserve qt = from_char_code(34)
        source = "blend factorial(n) " + lb + nl
        source = source + "    if n <= 1 " + lb + nl
        source = source + "        yield 1" + nl
        source = source + "    " + rb + nl
        source = source + "    n * factorial(n - 1)" + nl
        source = source + rb + nl + nl
        source = source + "blend main() " + lb + nl
        source = source + "    display(factorial(10))" + nl
        source = source + rb
    }

    preserve python_code = compile(source)
    display(trim_output(python_code))
}
