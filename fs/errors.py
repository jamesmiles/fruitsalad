"""Error hierarchy for the Fruit Salad language."""


class FruitSaladError(Exception):
    """Base error for all Fruit Salad errors."""

    def __init__(self, message: str, line: int = 0, column: int = 0):
        self.message = message
        self.line = line
        self.column = column
        super().__init__(self.format())

    def format(self) -> str:
        if self.line > 0:
            return f"[line {self.line}, col {self.column}] {self.message}"
        return self.message


class LexError(FruitSaladError):
    """Error during lexical analysis."""

    def format(self) -> str:
        loc = f"[line {self.line}, col {self.column}] " if self.line > 0 else ""
        return f"{loc}Lex Error: {self.message}"


class ParseError(FruitSaladError):
    """Error during parsing."""

    def format(self) -> str:
        loc = f"[line {self.line}, col {self.column}] " if self.line > 0 else ""
        return f"{loc}Parse Error: {self.message}"


class SplatError(FruitSaladError):
    """Runtime error (Splat! - fruit hit the floor)."""

    def format(self) -> str:
        loc = f"[line {self.line}, col {self.column}] " if self.line > 0 else ""
        return f"{loc}Splat! Runtime Error: {self.message}"


class RotError(FruitSaladError):
    """Type error (Rot - when fruit goes bad)."""

    def format(self) -> str:
        loc = f"[line {self.line}, col {self.column}] " if self.line > 0 else ""
        return f"{loc}Rot! Type Error: {self.message}"
