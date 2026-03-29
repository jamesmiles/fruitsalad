"""Recursive descent parser for the Fruit Salad language."""

from .tokens import Token, TokenType
from .ast_nodes import (
    Program, BlendDef, Block, PreserveDef, FreshDef, CandiedDef,
    Assignment, IfExpr, WhileExpr, EachExpr, LoopExpr,
    SnapStmt, SkipStmt, YieldStmt,
    BinaryExpr, UnaryExpr, CallExpr, IndexExpr, FieldExpr,
    NumberLiteral, StringLiteral, BoolLiteral, BasketLiteral, RangeLiteral,
    Identifier, StringInterpolation, DisplayExpr,
    BowlDef, BowlLiteral, MedleyDef, MedleyVariantExpr, SortExpr,
    SqueezeLiteral, PantryLiteral,
    SmoothieExpr, JuiceOrRotExpr, TossStmt, RecipeDef, PrepDef,
    PitLiteral, RipeExpr, RotExpr,
    PeelExpr, ToAppleExpr, ToDateExpr, ToBananaExpr, AbsExpr, MinExpr, MaxExpr,
)
from .errors import ParseError

# Token types that represent type names
TYPE_TOKENS = {
    TokenType.APPLE, TokenType.DATE, TokenType.BANANA,
    TokenType.CHERRY, TokenType.BASKET, TokenType.IDENTIFIER,
    TokenType.BOWL, TokenType.MEDLEY, TokenType.SORT,
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
            elif self.peek() == TokenType.BOWL:
                prog.statements.append(self.parse_bowl_def())
            elif self.peek() == TokenType.MEDLEY:
                prog.statements.append(self.parse_medley_def())
            elif self.peek() == TokenType.RECIPE:
                prog.statements.append(self.parse_recipe_def())
            elif self.peek() == TokenType.PREP:
                prog.statements.append(self.parse_prep_def())
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

        if tt == TokenType.BOWL:
            return self.parse_bowl_def()
        if tt == TokenType.MEDLEY:
            return self.parse_medley_def()
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
        if tt == TokenType.TOSS:
            return self.parse_toss()
        if tt == TokenType.PEEL:
            return self.parse_peel()
        if tt == TokenType.TO_APPLE:
            return self.parse_to_apple()
        if tt == TokenType.TO_DATE:
            return self.parse_to_date()
        if tt == TokenType.TO_BANANA:
            return self.parse_to_banana()
        if tt == TokenType.ABS:
            return self.parse_abs()
        if tt == TokenType.MIN:
            return self.parse_min()
        if tt == TokenType.MAX:
            return self.parse_max()
        if tt == TokenType.RECIPE:
            return self.parse_recipe_def()
        if tt == TokenType.PREP:
            return self.parse_prep_def()

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

    # --- Phase 2: Bowl, Medley, Sort, Squeeze ---

    def parse_bowl_def(self) -> BowlDef:
        tok = self.expect(TokenType.BOWL)
        name = self.expect(TokenType.IDENTIFIER, "Expected bowl name after 'bowl'").value
        self.expect(TokenType.LBRACE, "Expected '{' after bowl name")
        fields = []
        while self.peek() != TokenType.RBRACE and not self.at_end():
            field_name = self.expect(TokenType.IDENTIFIER, "Expected field name").value
            type_ann = None
            if self.match(TokenType.COLON):
                type_ann = self.parse_type_name()
            fields.append((field_name, type_ann))
            if not self.match(TokenType.COMMA):
                break
        self.expect(TokenType.RBRACE, "Expected '}' after bowl fields")
        return BowlDef(line=tok.line, column=tok.column, name=name, fields=fields)

    def parse_medley_def(self) -> MedleyDef:
        tok = self.expect(TokenType.MEDLEY)
        name = self.expect(TokenType.IDENTIFIER, "Expected medley name after 'medley'").value
        self.expect(TokenType.LBRACE, "Expected '{' after medley name")
        variants = []
        while self.peek() != TokenType.RBRACE and not self.at_end():
            var_name = self.expect(TokenType.IDENTIFIER, "Expected variant name").value
            var_fields = None
            if self.match(TokenType.LPAREN):
                var_fields = []
                if self.peek() != TokenType.RPAREN:
                    fname = self.expect(TokenType.IDENTIFIER, "Expected field name").value
                    ftype = None
                    if self.match(TokenType.COLON):
                        ftype = self.parse_type_name()
                    var_fields.append((fname, ftype))
                    while self.match(TokenType.COMMA):
                        fname = self.expect(TokenType.IDENTIFIER, "Expected field name").value
                        ftype = None
                        if self.match(TokenType.COLON):
                            ftype = self.parse_type_name()
                        var_fields.append((fname, ftype))
                self.expect(TokenType.RPAREN, "Expected ')' after variant fields")
            variants.append((var_name, var_fields))
            if not self.match(TokenType.COMMA):
                break
        self.expect(TokenType.RBRACE, "Expected '}' after medley variants")
        return MedleyDef(line=tok.line, column=tok.column, name=name, variants=variants)

    def parse_sort_expr(self) -> SortExpr:
        tok = self.advance()  # consume 'sort'
        subject = self.parse_expression()
        self.expect(TokenType.LBRACE, "Expected '{' after sort expression")
        arms = []
        while self.peek() != TokenType.RBRACE and not self.at_end():
            pattern = self.parse_sort_pattern()
            guard = None
            # Check for 'where' guard (identifier with value "where")
            if (self.peek() == TokenType.IDENTIFIER and
                    self.current().value == "where"):
                self.advance()  # consume 'where'
                guard = self.parse_expression()
            self.expect(TokenType.FAT_ARROW, "Expected '=>' after pattern")
            # Body: either a block or an expression
            if self.peek() == TokenType.LBRACE:
                body = self.parse_block()
            else:
                body = self.parse_expression()
            arms.append((pattern, guard, body))
            # Optional comma between arms
            self.match(TokenType.COMMA)
        self.expect(TokenType.RBRACE, "Expected '}' after sort arms")
        return SortExpr(line=tok.line, column=tok.column, subject=subject, arms=arms)

    def parse_sort_pattern(self):
        """Parse a pattern for sort arms.
        Patterns: literal, _, identifier, MedleyName.Variant(bindings)
        Returns a tuple describing the pattern.
        """
        tok = self.current()

        # Wildcard
        if tok.type == TokenType.IDENTIFIER and tok.value == "_":
            self.advance()
            return ("wildcard",)

        # Number literal (including negative)
        if tok.type == TokenType.MINUS:
            self.advance()
            num_tok = self.current()
            if num_tok.type in (TokenType.INTEGER, TokenType.FLOAT):
                self.advance()
                return ("literal", -num_tok.value)

        if tok.type in (TokenType.INTEGER, TokenType.FLOAT):
            self.advance()
            return ("literal", tok.value)

        if tok.type == TokenType.STRING:
            self.advance()
            return ("literal", tok.value)

        if tok.type == TokenType.TRUE:
            self.advance()
            return ("literal", True)

        if tok.type == TokenType.FALSE:
            self.advance()
            return ("literal", False)

        # ripe(binding) pattern
        if tok.type == TokenType.RIPE:
            self.advance()
            bindings = []
            if self.match(TokenType.LPAREN):
                if self.peek() != TokenType.RPAREN:
                    bindings.append(self.expect(TokenType.IDENTIFIER, "Expected binding name").value)
                    while self.match(TokenType.COMMA):
                        bindings.append(self.expect(TokenType.IDENTIFIER, "Expected binding name").value)
                self.expect(TokenType.RPAREN, "Expected ')' after ripe bindings")
            return ("variant", "Ripe", "ripe", bindings)

        # rot(binding) pattern
        if tok.type == TokenType.ROT:
            self.advance()
            bindings = []
            if self.match(TokenType.LPAREN):
                if self.peek() != TokenType.RPAREN:
                    bindings.append(self.expect(TokenType.IDENTIFIER, "Expected binding name").value)
                    while self.match(TokenType.COMMA):
                        bindings.append(self.expect(TokenType.IDENTIFIER, "Expected binding name").value)
                self.expect(TokenType.RPAREN, "Expected ')' after rot bindings")
            return ("variant", "Harvest", "rot", bindings)

        # pit pattern (Ripe.pit)
        if tok.type == TokenType.PIT:
            self.advance()
            return ("variant", "Ripe", "pit", [])

        # Identifier: could be a binding, or MedleyName.Variant
        if tok.type == TokenType.IDENTIFIER:
            name = tok.value
            self.advance()
            # Check for dot access: MedleyName.Variant
            if self.match(TokenType.DOT):
                variant_name = self.expect(TokenType.IDENTIFIER, "Expected variant name after '.'").value
                bindings = []
                if self.match(TokenType.LPAREN):
                    if self.peek() != TokenType.RPAREN:
                        bindings.append(self.expect(TokenType.IDENTIFIER, "Expected binding name").value)
                        while self.match(TokenType.COMMA):
                            bindings.append(self.expect(TokenType.IDENTIFIER, "Expected binding name").value)
                    self.expect(TokenType.RPAREN, "Expected ')' after variant bindings")
                return ("variant", name, variant_name, bindings)
            # Plain identifier = binding
            return ("binding", name)

        raise self.error(f"Unexpected token in sort pattern: {tok.type.name}")

    def parse_squeeze(self) -> SqueezeLiteral:
        """Parse a squeeze (lambda): |params| { body } or |params| expr"""
        tok = self.expect(TokenType.PIPE)
        params = []
        if self.peek() != TokenType.PIPE:
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
        self.expect(TokenType.PIPE, "Expected '|' after squeeze parameters")
        # Body: block or expression
        if self.peek() == TokenType.LBRACE:
            body = self.parse_block()
        else:
            body = self.parse_expression()
        return SqueezeLiteral(line=tok.line, column=tok.column, params=params, body=body)

    def parse_toss(self) -> TossStmt:
        tok = self.advance()  # consume 'toss'
        value = self.parse_expression()
        return TossStmt(line=tok.line, column=tok.column, value=value)

    # --- Phase 4: peel, conversion builtins ---

    def parse_peel(self) -> PeelExpr:
        tok = self.advance()
        self.expect(TokenType.LPAREN, "Expected '(' after 'peel'")
        value = self.parse_expression()
        self.expect(TokenType.RPAREN, "Expected ')' after peel argument")
        return PeelExpr(line=tok.line, column=tok.column, value=value)

    def parse_to_apple(self) -> ToAppleExpr:
        tok = self.advance()
        self.expect(TokenType.LPAREN, "Expected '(' after 'to_apple'")
        value = self.parse_expression()
        self.expect(TokenType.RPAREN, "Expected ')' after to_apple argument")
        return ToAppleExpr(line=tok.line, column=tok.column, value=value)

    def parse_to_date(self) -> ToDateExpr:
        tok = self.advance()
        self.expect(TokenType.LPAREN, "Expected '(' after 'to_date'")
        value = self.parse_expression()
        self.expect(TokenType.RPAREN, "Expected ')' after to_date argument")
        return ToDateExpr(line=tok.line, column=tok.column, value=value)

    def parse_to_banana(self) -> ToBananaExpr:
        tok = self.advance()
        self.expect(TokenType.LPAREN, "Expected '(' after 'to_banana'")
        value = self.parse_expression()
        self.expect(TokenType.RPAREN, "Expected ')' after to_banana argument")
        return ToBananaExpr(line=tok.line, column=tok.column, value=value)

    def parse_abs(self) -> AbsExpr:
        tok = self.advance()
        self.expect(TokenType.LPAREN, "Expected '(' after 'abs'")
        value = self.parse_expression()
        self.expect(TokenType.RPAREN, "Expected ')' after abs argument")
        return AbsExpr(line=tok.line, column=tok.column, value=value)

    def parse_min(self) -> MinExpr:
        tok = self.advance()
        self.expect(TokenType.LPAREN, "Expected '(' after 'min'")
        left = self.parse_expression()
        self.expect(TokenType.COMMA, "Expected ',' between min arguments")
        right = self.parse_expression()
        self.expect(TokenType.RPAREN, "Expected ')' after min arguments")
        return MinExpr(line=tok.line, column=tok.column, left=left, right=right)

    def parse_max(self) -> MaxExpr:
        tok = self.advance()
        self.expect(TokenType.LPAREN, "Expected '(' after 'max'")
        left = self.parse_expression()
        self.expect(TokenType.COMMA, "Expected ',' between max arguments")
        right = self.parse_expression()
        self.expect(TokenType.RPAREN, "Expected ')' after max arguments")
        return MaxExpr(line=tok.line, column=tok.column, left=left, right=right)

    def parse_recipe_def(self) -> RecipeDef:
        tok = self.expect(TokenType.RECIPE)
        name = self.expect(TokenType.IDENTIFIER, "Expected recipe name after 'recipe'").value
        self.expect(TokenType.LBRACE, "Expected '{' after recipe name")
        methods = []
        while self.peek() != TokenType.RBRACE and not self.at_end():
            methods.append(self.parse_blend_def())
        self.expect(TokenType.RBRACE, "Expected '}' after recipe methods")
        return RecipeDef(line=tok.line, column=tok.column, name=name, methods=methods)

    def parse_prep_def(self) -> PrepDef:
        tok = self.expect(TokenType.PREP)
        type_name = self.expect(TokenType.IDENTIFIER, "Expected type name after 'prep'").value
        self.expect(TokenType.AS, "Expected 'as' after type name in prep")
        recipe_name = self.expect(TokenType.IDENTIFIER, "Expected recipe name after 'as'").value
        self.expect(TokenType.LBRACE, "Expected '{' after recipe name in prep")
        methods = []
        while self.peek() != TokenType.RBRACE and not self.at_end():
            methods.append(self.parse_blend_def())
        self.expect(TokenType.RBRACE, "Expected '}' after prep methods")
        return PrepDef(line=tok.line, column=tok.column, type_name=type_name, recipe_name=recipe_name, methods=methods)

    def _is_bowl_literal(self) -> bool:
        """Lookahead: after seeing IDENTIFIER {, check if it's Name { field: ... }
        Returns True if this looks like a bowl literal, False for a block."""
        # We're positioned right after the identifier. Check if next is {
        if self.peek() != TokenType.LBRACE:
            return False
        # Lookahead: { IDENTIFIER : ... } => bowl literal
        if self.peek_at(1) == TokenType.IDENTIFIER and self.peek_at(2) == TokenType.COLON:
            return True
        # Empty bowl: { } could be a block, so we don't treat it as bowl
        return False

    def _parse_bowl_literal(self, name_tok: 'Token') -> BowlLiteral:
        """Parse bowl literal after the name identifier has been consumed.
        We're at the opening {."""
        self.expect(TokenType.LBRACE)
        field_values = []
        while self.peek() != TokenType.RBRACE and not self.at_end():
            fname = self.expect(TokenType.IDENTIFIER, "Expected field name").value
            self.expect(TokenType.COLON, "Expected ':' after field name in bowl literal")
            fvalue = self.parse_expression()
            field_values.append((fname, fvalue))
            if not self.match(TokenType.COMMA):
                break
        self.expect(TokenType.RBRACE, "Expected '}' after bowl literal")
        return BowlLiteral(
            line=name_tok.line, column=name_tok.column,
            name=name_tok.value, field_values=field_values,
        )

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
        left = self.parse_pipeline()
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

    def parse_pipeline(self):
        left = self.parse_range()
        while self.match(TokenType.SMOOTHIE):
            # Right side: parse a postfix expression (could be call or identifier)
            right = self.parse_postfix()
            left = SmoothieExpr(line=left.line, column=left.column, left=left, right=right)
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
                # Field/method access - allow some keywords as field names
                tok = self.current()
                if tok.type == TokenType.IDENTIFIER:
                    field_name = self.advance().value
                elif tok.type == TokenType.EACH:
                    field_name = self.advance().value
                elif tok.type == TokenType.SORT:
                    field_name = self.advance().value
                elif tok.type == TokenType.DISPLAY:
                    field_name = self.advance().value
                else:
                    field_name = self.expect(TokenType.IDENTIFIER, "Expected field name after '.'").value
                expr = FieldExpr(line=expr.line, column=expr.column, object=expr, field=field_name)
            elif self.match(TokenType.QUESTION):
                # Juice-or-rot postfix operator
                expr = JuiceOrRotExpr(line=expr.line, column=expr.column, expr=expr)
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
            # Check for bowl literal: Name { field: value, ... }
            if self._is_bowl_literal():
                return self._parse_bowl_literal(tok)
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

        # Sort expression
        if self.peek() == TokenType.SORT:
            return self.parse_sort_expr()

        # Display as expression
        if self.peek() == TokenType.DISPLAY:
            return self.parse_display()

        # Squeeze (lambda): |params| body
        if self.peek() == TokenType.PIPE:
            return self.parse_squeeze()

        # Phase 3: pit, ripe, rot
        if self.match(TokenType.PIT):
            return PitLiteral(line=tok.line, column=tok.column)

        if self.match(TokenType.RIPE):
            self.expect(TokenType.LPAREN, "Expected '(' after 'ripe'")
            value = self.parse_expression()
            self.expect(TokenType.RPAREN, "Expected ')' after ripe value")
            return RipeExpr(line=tok.line, column=tok.column, value=value)

        if self.match(TokenType.ROT):
            self.expect(TokenType.LPAREN, "Expected '(' after 'rot'")
            value = self.parse_expression()
            self.expect(TokenType.RPAREN, "Expected ')' after rot value")
            return RotExpr(line=tok.line, column=tok.column, value=value)

        # Phase 4: peel, conversions, math builtins as expressions
        if self.peek() == TokenType.PEEL:
            return self.parse_peel()
        if self.peek() == TokenType.TO_APPLE:
            return self.parse_to_apple()
        if self.peek() == TokenType.TO_DATE:
            return self.parse_to_date()
        if self.peek() == TokenType.TO_BANANA:
            return self.parse_to_banana()
        if self.peek() == TokenType.ABS:
            return self.parse_abs()
        if self.peek() == TokenType.MIN:
            return self.parse_min()
        if self.peek() == TokenType.MAX:
            return self.parse_max()

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
