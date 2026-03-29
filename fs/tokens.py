"""Token types and Token dataclass for the Fruit Salad lexer."""

from dataclasses import dataclass
from enum import Enum, auto


class TokenType(Enum):
    # Literals
    INTEGER = auto()
    FLOAT = auto()
    STRING = auto()
    TRUE = auto()
    FALSE = auto()

    # Identifiers and keywords
    IDENTIFIER = auto()
    BLEND = auto()
    PRESERVE = auto()
    FRESH = auto()
    CANDIED = auto()
    IF = auto()
    ELSE = auto()
    WHILE = auto()
    EACH = auto()
    IN = auto()
    LOOP = auto()
    SNAP = auto()       # break
    SKIP = auto()       # continue
    YIELD = auto()      # explicit return
    DISPLAY = auto()    # built-in print

    # Type keywords (for annotations, not enforced in Phase 1)
    APPLE = auto()      # int
    DATE = auto()       # float
    BANANA = auto()     # string
    CHERRY = auto()     # bool
    BASKET = auto()     # array

    # Phase 2 keywords
    BOWL = auto()       # struct
    MEDLEY = auto()     # enum
    SORT = auto()       # pattern matching
    PANTRY = auto()     # hash map type

    # Phase 3 keywords
    RIPE = auto()       # Option some
    PIT = auto()        # Option none (keyword constant)
    ROT = auto()        # Result error constructor
    TOSS = auto()       # throw/propagate error
    RECIPE = auto()     # trait definition
    PREP = auto()       # trait implementation
    AS = auto()         # used in "prep X as Y"

    # Phase 4 keywords
    PEEL = auto()       # type introspection
    TO_APPLE = auto()   # convert to int
    TO_DATE = auto()    # convert to float
    TO_BANANA = auto()  # convert to string
    ABS = auto()        # absolute value
    MIN = auto()        # min of two values
    MAX = auto()        # max of two values

    # Operators
    PLUS = auto()
    MINUS = auto()
    STAR = auto()
    SLASH = auto()
    PERCENT = auto()
    EQUAL_EQUAL = auto()
    BANG_EQUAL = auto()
    LESS = auto()
    GREATER = auto()
    LESS_EQUAL = auto()
    GREATER_EQUAL = auto()
    AND_AND = auto()
    OR_OR = auto()
    BANG = auto()
    ASSIGN = auto()     # =

    # Delimiters
    LPAREN = auto()
    RPAREN = auto()
    LBRACE = auto()
    RBRACE = auto()
    LBRACKET = auto()
    RBRACKET = auto()
    COMMA = auto()
    COLON = auto()
    ARROW = auto()      # ->
    SMOOTHIE = auto()   # ~>
    FAT_ARROW = auto()  # =>
    PIPE = auto()       # |
    DOT_DOT = auto()    # ..
    DOT_DOT_EQ = auto() # ..=
    DOT = auto()
    QUESTION = auto()   # ? (juice_or_rot postfix operator)

    # String interpolation markers
    STRING_INTERP_START = auto()  # string segment before {
    STRING_INTERP_MID = auto()    # string segment between } and {
    STRING_INTERP_END = auto()    # string segment after last }

    # Special
    EOF = auto()
    NEWLINE = auto()


@dataclass
class Token:
    type: TokenType
    value: object
    line: int
    column: int

    def __repr__(self) -> str:
        return f"Token({self.type.name}, {self.value!r}, {self.line}:{self.column})"
