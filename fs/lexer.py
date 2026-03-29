"""Lexer (tokenizer) for the Fruit Salad language."""

from .tokens import Token, TokenType
from .errors import LexError


KEYWORDS = {
    "blend": TokenType.BLEND,
    "preserve": TokenType.PRESERVE,
    "fresh": TokenType.FRESH,
    "candied": TokenType.CANDIED,
    "if": TokenType.IF,
    "else": TokenType.ELSE,
    "while": TokenType.WHILE,
    "each": TokenType.EACH,
    "in": TokenType.IN,
    "loop": TokenType.LOOP,
    "snap": TokenType.SNAP,
    "skip": TokenType.SKIP,
    "yield": TokenType.YIELD,
    "display": TokenType.DISPLAY,
    "true": TokenType.TRUE,
    "false": TokenType.FALSE,
    "Apple": TokenType.APPLE,
    "Date": TokenType.DATE,
    "Banana": TokenType.BANANA,
    "Cherry": TokenType.CHERRY,
    "Basket": TokenType.BASKET,
    "bowl": TokenType.BOWL,
    "medley": TokenType.MEDLEY,
    "sort": TokenType.SORT,
    "ripe": TokenType.RIPE,
    "pit": TokenType.PIT,
    "rot": TokenType.ROT,
    "toss": TokenType.TOSS,
    "recipe": TokenType.RECIPE,
    "prep": TokenType.PREP,
    "as": TokenType.AS,
    "peel": TokenType.PEEL,
    "to_apple": TokenType.TO_APPLE,
    "to_date": TokenType.TO_DATE,
    "to_banana": TokenType.TO_BANANA,
    "abs": TokenType.ABS,
    "min": TokenType.MIN,
    "max": TokenType.MAX,
}


class Lexer:
    def __init__(self, source: str, filename: str = "<stdin>"):
        self.source = source
        self.filename = filename
        self.pos = 0
        self.line = 1
        self.column = 1
        self.tokens: list[Token] = []

    def error(self, message: str) -> LexError:
        return LexError(message, self.line, self.column)

    def peek(self) -> str:
        if self.pos >= len(self.source):
            return "\0"
        return self.source[self.pos]

    def peek_next(self) -> str:
        if self.pos + 1 >= len(self.source):
            return "\0"
        return self.source[self.pos + 1]

    def advance(self) -> str:
        ch = self.source[self.pos]
        self.pos += 1
        if ch == "\n":
            self.line += 1
            self.column = 1
        else:
            self.column += 1
        return ch

    def match(self, expected: str) -> bool:
        if self.pos >= len(self.source) or self.source[self.pos] != expected:
            return False
        self.advance()
        return True

    def skip_whitespace(self):
        while self.pos < len(self.source) and self.source[self.pos] in " \t\r\n":
            self.advance()

    def skip_line_comment(self):
        # -- comment until end of line
        while self.pos < len(self.source) and self.source[self.pos] != "\n":
            self.advance()

    def skip_block_comment(self):
        # -{ ... }- with nesting support
        depth = 1
        while self.pos < len(self.source) and depth > 0:
            if self.source[self.pos] == "-" and self.pos + 1 < len(self.source) and self.source[self.pos + 1] == "{":
                depth += 1
                self.advance()
                self.advance()
            elif self.source[self.pos] == "}" and self.pos + 1 < len(self.source) and self.source[self.pos + 1] == "-":
                depth -= 1
                self.advance()
                self.advance()
            else:
                self.advance()
        if depth > 0:
            raise self.error("Unterminated block comment")

    def read_string(self) -> list[Token]:
        """Read a string literal, handling interpolation with { }."""
        start_line = self.line
        start_col = self.column
        self.advance()  # consume opening "

        tokens = []
        buf = []
        has_interp = False

        while self.pos < len(self.source) and self.source[self.pos] != '"':
            ch = self.source[self.pos]

            if ch == "\\" and self.pos + 1 < len(self.source):
                # Escape sequences
                self.advance()
                esc = self.advance()
                if esc == "n":
                    buf.append("\n")
                elif esc == "t":
                    buf.append("\t")
                elif esc == "\\":
                    buf.append("\\")
                elif esc == '"':
                    buf.append('"')
                elif esc == "{":
                    buf.append("{")
                elif esc == "}":
                    buf.append("}")
                else:
                    buf.append("\\")
                    buf.append(esc)
            elif ch == "{":
                # Start of interpolation
                has_interp = True
                seg = "".join(buf)
                buf = []
                tt = TokenType.STRING_INTERP_START if len(tokens) == 0 else TokenType.STRING_INTERP_MID
                tokens.append(Token(tt, seg, start_line, start_col))
                self.advance()  # consume {

                # Now lex tokens until matching }
                depth = 1
                while self.pos < len(self.source) and depth > 0:
                    self.skip_whitespace()
                    if self.pos >= len(self.source):
                        break
                    if self.source[self.pos] == "}":
                        depth -= 1
                        if depth == 0:
                            self.advance()  # consume closing }
                            break
                    if self.source[self.pos] == "{":
                        depth += 1
                    tok = self._next_token()
                    if tok.type == TokenType.EOF:
                        raise LexError("Unterminated string interpolation", start_line, start_col)
                    tokens.append(tok)

                start_line = self.line
                start_col = self.column
            else:
                buf.append(self.advance())

        if self.pos >= len(self.source):
            raise LexError("Unterminated string literal", start_line, start_col)
        self.advance()  # consume closing "

        seg = "".join(buf)
        if has_interp:
            tokens.append(Token(TokenType.STRING_INTERP_END, seg, start_line, start_col))
        else:
            tokens = [Token(TokenType.STRING, seg, start_line, start_col)]

        return tokens

    def read_number(self) -> Token:
        start_line = self.line
        start_col = self.column
        buf = []
        is_float = False

        while self.pos < len(self.source) and (self.source[self.pos].isdigit() or self.source[self.pos] == "_"):
            if self.source[self.pos] != "_":
                buf.append(self.source[self.pos])
            self.advance()

        # Check for float: digit followed by dot, but not .. (range)
        if (self.pos < len(self.source) and self.source[self.pos] == "."
                and self.pos + 1 < len(self.source) and self.source[self.pos + 1] != "."
                and self.source[self.pos + 1].isdigit()):
            is_float = True
            buf.append(self.advance())  # consume .
            while self.pos < len(self.source) and (self.source[self.pos].isdigit() or self.source[self.pos] == "_"):
                if self.source[self.pos] != "_":
                    buf.append(self.source[self.pos])
                self.advance()

        text = "".join(buf)
        if is_float:
            return Token(TokenType.FLOAT, float(text), start_line, start_col)
        else:
            return Token(TokenType.INTEGER, int(text), start_line, start_col)

    def read_identifier(self) -> Token:
        start_line = self.line
        start_col = self.column
        buf = []
        while self.pos < len(self.source) and (self.source[self.pos].isalnum() or self.source[self.pos] == "_"):
            buf.append(self.advance())
        text = "".join(buf)

        tt = KEYWORDS.get(text)
        if tt is not None:
            value = True if tt == TokenType.TRUE else (False if tt == TokenType.FALSE else text)
            return Token(tt, value, start_line, start_col)
        return Token(TokenType.IDENTIFIER, text, start_line, start_col)

    def _next_token(self) -> Token:
        """Lex a single token at current position (no whitespace skip)."""
        if self.pos >= len(self.source):
            return Token(TokenType.EOF, None, self.line, self.column)

        ch = self.source[self.pos]
        start_line = self.line
        start_col = self.column

        # Single character tokens
        simple = {
            "(": TokenType.LPAREN,
            ")": TokenType.RPAREN,
            "{": TokenType.LBRACE,
            "}": TokenType.RBRACE,
            "[": TokenType.LBRACKET,
            "]": TokenType.RBRACKET,
            ",": TokenType.COMMA,
            ":": TokenType.COLON,
            "+": TokenType.PLUS,
            "*": TokenType.STAR,
            "/": TokenType.SLASH,
            "%": TokenType.PERCENT,
        }

        if ch in simple:
            self.advance()
            return Token(simple[ch], ch, start_line, start_col)

        # Multi-character operators
        if ch == "-":
            self.advance()
            if self.match("-"):
                self.skip_line_comment()
                return None  # signal to skip
            if self.match("{"):
                self.skip_block_comment()
                return None
            if self.match(">"):
                return Token(TokenType.ARROW, "->", start_line, start_col)
            return Token(TokenType.MINUS, "-", start_line, start_col)

        if ch == "~":
            self.advance()
            if self.match(">"):
                return Token(TokenType.SMOOTHIE, "~>", start_line, start_col)
            raise self.error(f"Unexpected character '~'")

        if ch == "=":
            self.advance()
            if self.match("="):
                return Token(TokenType.EQUAL_EQUAL, "==", start_line, start_col)
            if self.match(">"):
                return Token(TokenType.FAT_ARROW, "=>", start_line, start_col)
            return Token(TokenType.ASSIGN, "=", start_line, start_col)

        if ch == "!":
            self.advance()
            if self.match("="):
                return Token(TokenType.BANG_EQUAL, "!=", start_line, start_col)
            return Token(TokenType.BANG, "!", start_line, start_col)

        if ch == "<":
            self.advance()
            if self.match("="):
                return Token(TokenType.LESS_EQUAL, "<=", start_line, start_col)
            return Token(TokenType.LESS, "<", start_line, start_col)

        if ch == ">":
            self.advance()
            if self.match("="):
                return Token(TokenType.GREATER_EQUAL, ">=", start_line, start_col)
            return Token(TokenType.GREATER, ">", start_line, start_col)

        if ch == "&":
            self.advance()
            if self.match("&"):
                return Token(TokenType.AND_AND, "&&", start_line, start_col)
            raise self.error("Unexpected character '&'. Did you mean '&&'?")

        if ch == "|":
            self.advance()
            if self.match("|"):
                return Token(TokenType.OR_OR, "||", start_line, start_col)
            return Token(TokenType.PIPE, "|", start_line, start_col)

        if ch == "?":
            self.advance()
            return Token(TokenType.QUESTION, "?", start_line, start_col)

        if ch == ".":
            self.advance()
            if self.match("."):
                if self.match("="):
                    return Token(TokenType.DOT_DOT_EQ, "..=", start_line, start_col)
                return Token(TokenType.DOT_DOT, "..", start_line, start_col)
            return Token(TokenType.DOT, ".", start_line, start_col)

        # String literal
        if ch == '"':
            return None  # handled specially

        # Number literal
        if ch.isdigit():
            return self.read_number()

        # Identifier or keyword
        if ch.isalpha() or ch == "_":
            return self.read_identifier()

        raise self.error(f"Unexpected character '{ch}'")

    def tokenize(self) -> list[Token]:
        """Tokenize the entire source into a list of tokens."""
        self.tokens = []

        while self.pos < len(self.source):
            self.skip_whitespace()
            if self.pos >= len(self.source):
                break

            ch = self.source[self.pos]

            # Handle strings specially due to interpolation
            if ch == '"':
                string_tokens = self.read_string()
                self.tokens.extend(string_tokens)
                continue

            # Handle comments inline since _next_token returns None for them
            tok = self._next_token()
            if tok is None:
                continue  # comment was skipped
            self.tokens.append(tok)

        self.tokens.append(Token(TokenType.EOF, None, self.line, self.column))
        return self.tokens
