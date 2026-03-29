-- @test ffs-001: Self-hosting compiler basic test
-- @expect def main():
-- @expect     print("Hello from ffs!")
-- @expect main()

-- Minimal test of the ffs compiler pipeline
-- This is a stripped-down version that tests tokenize -> parse -> generate

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

blend mk_tok(typ, val, line) {
    yield [typ, val, line]
}

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
    false
}

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
        if is_whitespace(c) {
            if c == "\n" {
                line = line + 1
            }
            pos = pos + 1
            skip
        }
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
        if c == "\"" {
            pos = pos + 1
            fresh buf = ""
            loop {
                if pos >= slen || source[pos] == "\"" {
                    snap
                }
                if source[pos] == "\\" && pos + 1 < slen {
                    pos = pos + 1
                    fresh esc = source[pos]
                    if esc == "n" {
                        buf = buf + "\n"
                    } else if esc == "\"" {
                        buf = buf + "\""
                    } else {
                        buf = buf + esc
                    }
                    pos = pos + 1
                    skip
                }
                buf = buf + source[pos]
                pos = pos + 1
            }
            if pos < slen {
                pos = pos + 1
            }
            tokens.push(mk_tok("STR", buf, line))
            skip
        }
        if is_digit(c) {
            fresh num_str = ""
            loop {
                if pos >= slen || !is_digit(source[pos]) {
                    snap
                }
                num_str = num_str + source[pos]
                pos = pos + 1
            }
            tokens.push(mk_tok("NUM", to_apple(num_str), line))
            skip
        }
        if is_alpha(c) {
            fresh word = ""
            loop {
                if pos >= slen || !is_alnum(source[pos]) {
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
        if c == "(" || c == ")" || c == "\{" || c == "\}" || c == "[" || c == "]" || c == "," || c == "." {
            tokens.push(mk_tok("DELIM", c, line))
            pos = pos + 1
            skip
        }
        if c == "+" || c == "-" || c == "*" || c == "/" || c == "%" || c == "=" || c == "<" || c == ">" || c == "!" {
            tokens.push(mk_tok("OP", c, line))
            pos = pos + 1
            skip
        }
        pos = pos + 1
    }
    tokens.push(mk_tok("EOF", "", line))
    yield tokens
}

-- Parser helpers
blend parser_peek(tokens, pos_ref) {
    yield tokens[pos_ref[0]]
}

blend parser_advance(tokens, pos_ref) {
    preserve tok = tokens[pos_ref[0]]
    pos_ref[0] = pos_ref[0] + 1
    yield tok
}

blend parser_expect(tokens, pos_ref, typ, val) {
    preserve tok = parser_peek(tokens, pos_ref)
    parser_advance(tokens, pos_ref)
}

blend parser_match_delim(tokens, pos_ref, d) {
    preserve tok = parser_peek(tokens, pos_ref)
    if tok[0] == "DELIM" && tok[1] == d {
        parser_advance(tokens, pos_ref)
        yield true
    }
    false
}

-- Minimal parser: just parse blend main() \{ display("...") \}
blend parse(tokens) {
    fresh pos_ref = [0]
    fresh functions = []
    -- parse blend def
    parser_expect(tokens, pos_ref, "KW", "blend")
    preserve name_tok = parser_advance(tokens, pos_ref)
    parser_expect(tokens, pos_ref, "DELIM", "(")
    parser_expect(tokens, pos_ref, "DELIM", ")")
    parser_expect(tokens, pos_ref, "DELIM", "\{")
    -- parse display("...")
    parser_expect(tokens, pos_ref, "KW", "display")
    parser_expect(tokens, pos_ref, "DELIM", "(")
    preserve str_tok = parser_advance(tokens, pos_ref)
    parser_expect(tokens, pos_ref, "DELIM", ")")
    parser_expect(tokens, pos_ref, "DELIM", "\}")

    preserve display_node = ["Display", [["StrLit", str_tok[1]]]]
    preserve body = ["Block", [display_node]]
    preserve func = ["BlendDef", name_tok[1], [], body]
    functions.push(func)
    yield ["Program", functions, []]
}

-- Code generator
blend py_escape_str(s) {
    fresh result = ""
    fresh i = 0
    while i < s.len() {
        fresh c = s[i]
        if c == "\\" {
            result = result + "\\\\"
        } else if c == "\"" {
            result = result + "\\\""
        } else {
            result = result + c
        }
        i = i + 1
    }
    yield result
}

blend make_indent(level) {
    fresh result = ""
    fresh i = 0
    while i < level {
        result = result + "    "
        i = i + 1
    }
    yield result
}

blend gen_expr(node) {
    preserve typ = node[0]
    if typ == "StrLit" {
        yield "\"" + py_escape_str(node[1]) + "\""
    }
    if typ == "NumLit" {
        yield to_banana(node[1])
    }
    yield ""
}

blend generate(node, level) {
    preserve typ = node[0]
    if typ == "Program" {
        fresh result = ""
        preserve funcs = node[1]
        each i in 0..funcs.len() {
            result = result + generate(funcs[i], level) + "\n"
        }
        each i in 0..funcs.len() {
            if funcs[i][1] == "main" {
                result = result + "main()\n"
            }
        }
        yield result
    }
    if typ == "BlendDef" {
        preserve name = node[1]
        preserve params = node[2]
        preserve body = node[3]
        fresh param_str = ""
        each i in 0..params.len() {
            if i > 0 {
                param_str = param_str + ", "
            }
            param_str = param_str + params[i]
        }
        fresh result = make_indent(level) + "def " + name + "(" + param_str + "):\n"
        preserve stmts = body[1]
        each i in 0..stmts.len() {
            result = result + generate(stmts[i], level + 1) + "\n"
        }
        yield result
    }
    if typ == "Display" {
        preserve args = node[1]
        fresh arg_strs = ""
        each i in 0..args.len() {
            if i > 0 {
                arg_strs = arg_strs + ", "
            }
            arg_strs = arg_strs + gen_expr(args[i])
        }
        yield make_indent(level) + "print(" + arg_strs + ")"
    }
    yield ""
}

blend main() {
    preserve source = "blend main() \{\n    display(\"Hello from ffs!\")\n\}"

    preserve tokens = tokenize(source)
    preserve ast = parse(tokens)
    preserve python_code = generate(ast, 0)

    fresh output = ""
    preserve lines = python_code.split("\n")
    fresh first = true
    each i in 0..lines.len() {
        if lines[i].trim() != "" {
            if !first {
                output = output + "\n"
            }
            output = output + lines[i]
            first = false
        }
    }
    display(output)
}
