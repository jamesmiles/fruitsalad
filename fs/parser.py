"""Recursive descent parser for the Fruit Salad language."""

from .tokens import Token, TokenType
from .ast_nodes import (
    Program, BlendDef, Block, PreserveDef, FreshDef, CandiedDef,
    Assignment, IfExpr, WhileExpr, EachExpr, LoopExpr,
    SnapStmt, SkipStmt, YieldStmt,
    BinaryExpr, UnaryExpr, CallExpr, IndexExpr, FieldExpr,
    NumberLiteral, StringLiteral, BoolLiteral, BasketLiteral, RangeLiteral,
    Identifier, StringInterpolation, DisplayExpr,
)
from .errors import ParseError

# Token types that represent type names
TYPE_TOKENS = {
    TokenType.APPLE, TokenType.DATE, TokenType.BANANA,
    TokenType.CHERRY, TokenType.BASKET, TokenType.IDENTIFIER,
}


class Parser:
    def __init__(self, tokens: list[Token]):
        self.tokens = tokens
        self.pos = 0

    # --- Utilities ---

    def current(self) -> Token:
        return self.tokens[self.pos]

    def peek(self) -> TokenType:
        return self.tokens[self.pos].type

    def peek_at(self, offset: int) -> TokenType:
        idx = self.pos + offset
        if idx >= len(self.tokens):
            return TokenType.EOF
        return self.tokens[idx].type

    def at_end(self) -> bool:
        return self.peek() == TokenType.EOF

    def advance(self) -> Token:
        tok = self.tokens[self.pos]
        if not self.at_end():
            self.pos += 1
        return tok

    def expect(self, tt: TokenType, msg: str = "") -> Token:
        tok = self.current()
        if tok.type != tt:
            if not msg:
                msg = f"Expected {tt.name}, got {tok.type.name} ({tok.value!r})"
            raise ParseError(msg, tok.line, tok.column)
        return self.advance()

    def match(self, *types: TokenType) -> Token | None:
        if self.peek() in types:
            return self.advance()
        return None

    def error(self, msg: str) -> ParseError:
        tok = self.current()
        return ParseError(msg, tok.line, tok.column)

    def parse_type_name(self) -> str:
        """Parse a type name (identifier or type keyword like Apple, Banana, etc.)."""
        tok = self.current()
        if tok.type in TYPE_TOKENS:
            self.advance()
            return tok.value if tok.type == TokenType.IDENTIFIER else tok.type.name.capitalize()
        raise self.error(f"Expected type name, got {tok.type.name}")

    # --- Grammar ---

    def parse(self) -> Program:
        prog = Program()
        while not self.at_end():
            if self.peek() == TokenType.BLEND:
                prog.functions.append(self.parse_blend_def())
            else:
                prog.statements.append(self.parse_statement())
        return prog

    def parse_blend_def(self) -> BlendDef:
        tok = self.expect(TokenType.BLEND)
        name_tok = self.expect(TokenType.IDENTIFIER, "Expected function name after 'blend'")
        self.expect(TokenType.LPAREN, "Expected '(' after function name")
        params = self.parse_param_list()
        self.expect(TokenType.RPAREN, "Expected ')' after parameters")

        return_type = None
        if self.match(TokenType.ARROW):
            return_type = self.parse_type_name()

        body = self.parse_block()
        return BlendDef(
            line=tok.line, column=tok.column,
            name=name_tok.value, params=params,
            return_type=return_type, body=body,
        )

    def parse_param_list(self) -> list:
        params = []
        if self.peek() == TokenType.RPAREN:
            return params
        # first param
        name = self.expect(TokenType.IDENTIFIER, "Expected parameter name").value
        type_ann = None
        if self.match(TokenType.COLON):
            type_ann = self.parse_type_name()
        params.append((name, type_ann))
        while self.match(TokenType.COMMA):
            name = self.expect(TokenType.IDENTIFIER, "Expected parameter name").value
            type_ann = None
            if self.match(TokenType.COLON):
                type_ann = self.parse_type_name()
            params.append((name, type_ann))
        return params

    def parse_block(self) -> Block:
        tok = self.expect(TokenType.LBRACE, "Expected '{' to open block")
        stmts = []
        while self.peek() != TokenType.RBRACE and not self.at_end():
            stmts.append(self.parse_statement())
        self.expect(TokenType.RBRACE, "Expected '}' to close block")
        return Block(line=tok.line, column=tok.column, statements=stmts)

    def parse_statement(self):
        tt = self.peek()

        if tt == TokenType.PRESERVE:
            return self.parse_preserve()
        if tt == TokenType.FRESH:
            return self.parse_fresh()
        if tt == TokenType.CANDIED:
            return self.parse_candied()
        if tt == TokenType.IF:
            return self.parse_if()
        if tt == TokenType.WHILE:
            return self.parse_while()
        if tt == TokenType.EACH:
            return self.parse_each()
        if tt == TokenType.LOOP:
            return self.parse_loop()
        if tt == TokenType.SNAP:
            tok = self.advance()
            return SnapStmt(line=tok.line, column=tok.column)
        if tt == TokenType.SKIP:
            tok = self.advance()
            return SkipStmt(line=tok.line, column=tok.column)
        if tt == TokenType.YIELD:
            return self.parse_yield()
        if tt == TokenType.DISPLAY:
            return self.parse_display()

        # Expression statement (possibly assignment)
        expr = self.parse_expression()

        # Check for assignment: expr = value
        if self.match(TokenType.ASSIGN):
            value = self.parse_expression()
            return Assignment(
                line=expr.line, column=expr.column,
                target=expr, value=value,
            )

        return expr

    def parse_preserve(self) -> PreserveDef:
        tok = self.advance()
        name = self.expect(TokenType.IDENTIFIER, "Expected variable name after 'preserve'").value
        type_ann = None
        if self.match(TokenType.COLON):
            type_ann = self.parse_type_name()
        self.expect(TokenType.ASSIGN, "Expected '=' in preserve declaration")
        value = self.parse_expression()
        return PreserveDef(line=tok.line, column=tok.column, name=name, type_annotation=type_ann, value=value)

    def parse_fresh(self) -> FreshDef:
        tok = self.advance()
        name = self.expect(TokenType.IDENTIFIER, "Expected variable name after 'fresh'").value
        type_ann = None
        if self.match(TokenType.COLON):
            type_ann = self.parse_type_name()
        self.expect(TokenType.ASSIGN, "Expected '=' in fresh declaration")
        value = self.parse_expression()
        return FreshDef(line=tok.line, column=tok.column, name=name, type_annotation=type_ann, value=value)

    def parse_candied(self) -> CandiedDef:
        tok = self.advance()
        name = self.expect(TokenType.IDENTIFIER, "Expected constant name after 'candied'").value
        type_ann = None
        if self.match(TokenType.COLON):
            type_ann = self.parse_type_name()
        self.expect(TokenType.ASSIGN, "Expected '=' in candied declaration")
        value = self.parse_expression()
        return CandiedDef(line=tok.line, column=tok.column, name=name, type_annotation=type_ann, value=value)

    def parse_if(self) -> IfExpr:
        tok = self.advance()  # consume 'if'
        condition = self.parse_expression()
        then_block = self.parse_block()
        else_block = None
        if self.match(TokenType.ELSE):
            if self.peek() == TokenType.IF:
                else_block = self.parse_if()
            else:
                else_block = self.parse_block()
        return IfExpr(
            line=tok.line, column=tok.column,
            condition=condition, then_block=then_block, else_block=else_block,
        )

    def parse_while(self) -> WhileExpr:
        tok = self.advance()
        condition = self.parse_expression()
        body = self.parse_block()
        return WhileExpr(line=tok.line, column=tok.column, condition=condition, body=body)

    def parse_each(self) -> EachExpr:
        tok = self.advance()  # consume 'each'
        var_name = self.expect(TokenType.IDENTIFIER, "Expected variable name after 'each'").value
        self.expect(TokenType.IN, "Expected 'in' after each variable")
        iterable = self.parse_expression()
        body = self.parse_block()
        return EachExpr(line=tok.line, column=tok.column, variable=var_name, iterable=iterable, body=body)

    def parse_loop(self) -> LoopExpr:
        tok = self.advance()
        body = self.parse_block()
        return LoopExpr(line=tok.line, column=tok.column, body=body)

    def parse_yield(self) -> YieldStmt:
        tok = self.advance()
        # yield can be followed by an expression, or stand alone
        if self.peek() in (TokenType.RBRACE, TokenType.EOF):
            return YieldStmt(line=tok.line, column=tok.column, value=None)
        value = self.parse_expression()
        return YieldStmt(line=tok.line, column=tok.column, value=value)

    def parse_display(self) -> DisplayExpr:
        tok = self.advance()
        self.expect(TokenType.LPAREN, "Expected '(' after 'display'")
        args = []
        if self.peek() != TokenType.RPAREN:
            args.append(self.parse_expression())
            while self.match(TokenType.COMMA):
                args.append(self.parse_expression())
        self.expect(TokenType.RPAREN, "Expected ')' after display arguments")
        return DisplayExpr(line=tok.line, column=tok.column, args=args)

    # --- Expression parsing with precedence ---

    def parse_expression(self):
        return self.parse_or()

    def parse_or(self):
        left = self.parse_and()
        while self.match(TokenType.OR_OR):
            right = self.parse_and()
            left = BinaryExpr(line=left.line, column=left.column, left=left, op="||", right=right)
        return left

    def parse_and(self):
        left = self.parse_equality()
        while self.match(TokenType.AND_AND):
            right = self.parse_equality()
            left = BinaryExpr(line=left.line, column=left.column, left=left, op="&&", right=right)
        return left

    def parse_equality(self):
        left = self.parse_comparison()
        while True:
            if self.match(TokenType.EQUAL_EQUAL):
                right = self.parse_comparison()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="==", right=right)
            elif self.match(TokenType.BANG_EQUAL):
                right = self.parse_comparison()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="!=", right=right)
            else:
                break
        return left

    def parse_comparison(self):
        left = self.parse_range()
        while True:
            if self.match(TokenType.LESS):
                right = self.parse_range()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="<", right=right)
            elif self.match(TokenType.GREATER):
                right = self.parse_range()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op=">", right=right)
            elif self.match(TokenType.LESS_EQUAL):
                right = self.parse_range()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="<=", right=right)
            elif self.match(TokenType.GREATER_EQUAL):
                right = self.parse_range()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op=">=", right=right)
            else:
                break
        return left

    def parse_range(self):
        left = self.parse_addition()
        if self.match(TokenType.DOT_DOT_EQ):
            right = self.parse_addition()
            return RangeLiteral(line=left.line, column=left.column, start=left, end=right, inclusive=True)
        if self.match(TokenType.DOT_DOT):
            right = self.parse_addition()
            return RangeLiteral(line=left.line, column=left.column, start=left, end=right, inclusive=False)
        return left

    def parse_addition(self):
        left = self.parse_multiplication()
        while True:
            if self.match(TokenType.PLUS):
                right = self.parse_multiplication()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="+", right=right)
            elif self.match(TokenType.MINUS):
                right = self.parse_multiplication()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="-", right=right)
            else:
                break
        return left

    def parse_multiplication(self):
        left = self.parse_unary()
        while True:
            if self.match(TokenType.STAR):
                right = self.parse_unary()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="*", right=right)
            elif self.match(TokenType.SLASH):
                right = self.parse_unary()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="/", right=right)
            elif self.match(TokenType.PERCENT):
                right = self.parse_unary()
                left = BinaryExpr(line=left.line, column=left.column, left=left, op="%", right=right)
            else:
                break
        return left

    def parse_unary(self):
        if self.match(TokenType.MINUS):
            tok = self.tokens[self.pos - 1]
            operand = self.parse_unary()
            return UnaryExpr(line=tok.line, column=tok.column, op="-", operand=operand)
        if self.match(TokenType.BANG):
            tok = self.tokens[self.pos - 1]
            operand = self.parse_unary()
            return UnaryExpr(line=tok.line, column=tok.column, op="!", operand=operand)
        return self.parse_postfix()

    def parse_postfix(self):
        expr = self.parse_primary()
        while True:
            if self.match(TokenType.LPAREN):
                # Function call
                args = []
                if self.peek() != TokenType.RPAREN:
                    args.append(self.parse_expression())
                    while self.match(TokenType.COMMA):
                        args.append(self.parse_expression())
                self.expect(TokenType.RPAREN, "Expected ')' after function arguments")
                expr = CallExpr(line=expr.line, column=expr.column, callee=expr, args=args)
            elif self.match(TokenType.LBRACKET):
                # Index access
                index = self.parse_expression()
                self.expect(TokenType.RBRACKET, "Expected ']' after index")
                expr = IndexExpr(line=expr.line, column=expr.column, object=expr, index=index)
            elif self.match(TokenType.DOT):
                # Field/method access
                field_name = self.expect(TokenType.IDENTIFIER, "Expected field name after '.'").value
                expr = FieldExpr(line=expr.line, column=expr.column, object=expr, field=field_name)
            else:
                break
        return expr

    def parse_primary(self):
        tok = self.current()

        if self.match(TokenType.INTEGER):
            return NumberLiteral(line=tok.line, column=tok.column, value=tok.value)

        if self.match(TokenType.FLOAT):
            return NumberLiteral(line=tok.line, column=tok.column, value=tok.value)

        if self.match(TokenType.STRING):
            return StringLiteral(line=tok.line, column=tok.column, value=tok.value)

        if self.match(TokenType.TRUE):
            return BoolLiteral(line=tok.line, column=tok.column, value=True)

        if self.match(TokenType.FALSE):
            return BoolLiteral(line=tok.line, column=tok.column, value=False)

        if self.match(TokenType.IDENTIFIER):
            return Identifier(line=tok.line, column=tok.column, name=tok.value)

        # String interpolation
        if self.match(TokenType.STRING_INTERP_START):
            return self.parse_string_interpolation(tok)

        # Basket literal [...]
        if self.match(TokenType.LBRACKET):
            return self.parse_basket_literal(tok)

        # Parenthesized expression
        if self.match(TokenType.LPAREN):
            expr = self.parse_expression()
            self.expect(TokenType.RPAREN, "Expected ')' after expression")
            return expr

        # If expression in expression position
        if self.peek() == TokenType.IF:
            return self.parse_if()

        raise self.error(f"Unexpected token {tok.type.name} ({tok.value!r})")

    def parse_string_interpolation(self, start_tok: Token) -> StringInterpolation:
        parts = []
        # start_tok is STRING_INTERP_START with the leading text
        if start_tok.value:
            parts.append(start_tok.value)

        # Now read expression tokens until STRING_INTERP_MID or STRING_INTERP_END
        expr = self.parse_expression()
        parts.append(expr)

        while self.peek() == TokenType.STRING_INTERP_MID:
            mid = self.advance()
            if mid.value:
                parts.append(mid.value)
            expr = self.parse_expression()
            parts.append(expr)

        end = self.expect(TokenType.STRING_INTERP_END, "Expected end of interpolated string")
        if end.value:
            parts.append(end.value)

        return StringInterpolation(line=start_tok.line, column=start_tok.column, parts=parts)

    def parse_basket_literal(self, tok: Token) -> BasketLiteral:
        elements = []
        if self.peek() != TokenType.RBRACKET:
            elements.append(self.parse_expression())
            while self.match(TokenType.COMMA):
                if self.peek() == TokenType.RBRACKET:
                    break  # trailing comma
                elements.append(self.parse_expression())
        self.expect(TokenType.RBRACKET, "Expected ']' after basket elements")
        return BasketLiteral(line=tok.line, column=tok.column, elements=elements)
