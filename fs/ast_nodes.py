"""AST node definitions for the Fruit Salad language."""

from dataclasses import dataclass, field
from typing import Optional


@dataclass
class ASTNode:
    line: int = 0
    column: int = 0


# --- Top level ---

@dataclass
class Program(ASTNode):
    functions: list = field(default_factory=list)      # list of BlendDef
    statements: list = field(default_factory=list)      # top-level statements (if any)


@dataclass
class BlendDef(ASTNode):
    name: str = ""
    params: list = field(default_factory=list)          # list of (name, type_annotation?)
    return_type: Optional[str] = None
    body: "Block" = None


@dataclass
class Block(ASTNode):
    statements: list = field(default_factory=list)


# --- Variable declarations ---

@dataclass
class PreserveDef(ASTNode):
    """Immutable variable declaration: preserve x = expr"""
    name: str = ""
    type_annotation: Optional[str] = None
    value: ASTNode = None


@dataclass
class FreshDef(ASTNode):
    """Mutable variable declaration: fresh x = expr"""
    name: str = ""
    type_annotation: Optional[str] = None
    value: ASTNode = None


@dataclass
class CandiedDef(ASTNode):
    """Compile-time constant: candied X = expr"""
    name: str = ""
    type_annotation: Optional[str] = None
    value: ASTNode = None


@dataclass
class Assignment(ASTNode):
    """Assignment to existing variable: x = expr"""
    target: ASTNode = None
    value: ASTNode = None


# --- Control flow ---

@dataclass
class IfExpr(ASTNode):
    condition: ASTNode = None
    then_block: Block = None
    else_block: Optional[ASTNode] = None  # Block or IfExpr (for else if)


@dataclass
class WhileExpr(ASTNode):
    condition: ASTNode = None
    body: Block = None


@dataclass
class EachExpr(ASTNode):
    variable: str = ""
    iterable: ASTNode = None
    body: Block = None


@dataclass
class LoopExpr(ASTNode):
    body: Block = None


@dataclass
class SnapStmt(ASTNode):
    """Break statement."""
    pass


@dataclass
class SkipStmt(ASTNode):
    """Continue statement."""
    pass


@dataclass
class YieldStmt(ASTNode):
    """Explicit return: yield expr"""
    value: Optional[ASTNode] = None


# --- Expressions ---

@dataclass
class BinaryExpr(ASTNode):
    left: ASTNode = None
    op: str = ""
    right: ASTNode = None


@dataclass
class UnaryExpr(ASTNode):
    op: str = ""
    operand: ASTNode = None


@dataclass
class CallExpr(ASTNode):
    callee: ASTNode = None
    args: list = field(default_factory=list)


@dataclass
class IndexExpr(ASTNode):
    object: ASTNode = None
    index: ASTNode = None


@dataclass
class FieldExpr(ASTNode):
    """Field/method access: obj.field"""
    object: ASTNode = None
    field: str = ""


# --- Literals ---

@dataclass
class NumberLiteral(ASTNode):
    value: object = None  # int or float


@dataclass
class StringLiteral(ASTNode):
    value: str = ""


@dataclass
class BoolLiteral(ASTNode):
    value: bool = False


@dataclass
class BasketLiteral(ASTNode):
    elements: list = field(default_factory=list)


@dataclass
class RangeLiteral(ASTNode):
    start: ASTNode = None
    end: ASTNode = None
    inclusive: bool = False


@dataclass
class Identifier(ASTNode):
    name: str = ""


@dataclass
class StringInterpolation(ASTNode):
    """String with interpolated expressions.
    parts is a list of alternating string segments and expression AST nodes.
    """
    parts: list = field(default_factory=list)


@dataclass
class DisplayExpr(ASTNode):
    """Built-in display() call."""
    args: list = field(default_factory=list)


# --- Phase 2: Bowls, Medleys, Sort, Squeeze ---

@dataclass
class BowlDef(ASTNode):
    """Bowl (struct) definition: bowl Name { field1: Type, field2: Type }"""
    name: str = ""
    fields: list = field(default_factory=list)  # list of (name, type_annotation?)

@dataclass
class BowlLiteral(ASTNode):
    """Bowl instantiation: Name { field1: value1, field2: value2 }"""
    name: str = ""
    field_values: list = field(default_factory=list)  # list of (name, value_expr)

@dataclass
class MedleyDef(ASTNode):
    """Medley (enum) definition: medley Name { Variant1, Variant2(field: Type) }"""
    name: str = ""
    variants: list = field(default_factory=list)  # list of (variant_name, fields?)

@dataclass
class MedleyVariantExpr(ASTNode):
    """Medley variant construction: Name.Variant(args) or Name.Variant"""
    medley_name: str = ""
    variant_name: str = ""
    args: list = field(default_factory=list)

@dataclass
class SortExpr(ASTNode):
    """Pattern matching: sort expr { pattern => result, ... }"""
    subject: ASTNode = None
    arms: list = field(default_factory=list)  # list of (pattern, guard?, body)

@dataclass
class SqueezeLiteral(ASTNode):
    """Lambda/closure: |params| { body } or |params| expr"""
    params: list = field(default_factory=list)  # list of (name, type_ann?)
    body: ASTNode = None  # Block or expression

@dataclass
class PantryLiteral(ASTNode):
    """Pantry (map) literal: {key: value, key: value}"""
    entries: list = field(default_factory=list)  # list of (key_expr, value_expr)


# --- Phase 3: Smoothie, Ripe/Rot, Recipe/Prep, JuiceOrRot ---

@dataclass
class SmoothieExpr(ASTNode):
    """Pipeline operator: expr ~> func"""
    left: ASTNode = None
    right: ASTNode = None  # should be a callable expression

@dataclass
class JuiceOrRotExpr(ASTNode):
    """Postfix ? operator: unwrap Ripe or propagate Rot/Pit."""
    expr: ASTNode = None

@dataclass
class TossStmt(ASTNode):
    """Toss (throw): toss expr"""
    value: ASTNode = None

@dataclass
class RecipeDef(ASTNode):
    """Recipe (trait) definition: recipe Name { blend method(self) -> Type }"""
    name: str = ""
    methods: list = field(default_factory=list)  # list of BlendDef (signatures only, body may be None)

@dataclass
class PrepDef(ASTNode):
    """Prep (impl) block: prep TypeName as RecipeName { blend method(self) { ... } }"""
    type_name: str = ""
    recipe_name: str = ""
    methods: list = field(default_factory=list)  # list of BlendDef

@dataclass
class PitLiteral(ASTNode):
    """The pit constant (None/empty option)."""
    pass

@dataclass
class RipeExpr(ASTNode):
    """ripe(value) - wraps in Ripe."""
    value: ASTNode = None

@dataclass
class RotExpr(ASTNode):
    """rot(message) - creates Rot error value."""
    value: ASTNode = None


# --- Phase 4: Peel, conversion builtins ---

@dataclass
class PeelExpr(ASTNode):
    """peel(expr) - returns type name as string."""
    value: ASTNode = None

@dataclass
class ToAppleExpr(ASTNode):
    """to_apple(expr) - convert to int."""
    value: ASTNode = None

@dataclass
class ToDateExpr(ASTNode):
    """to_date(expr) - convert to float."""
    value: ASTNode = None

@dataclass
class ToBananaExpr(ASTNode):
    """to_banana(expr) - convert to string."""
    value: ASTNode = None

@dataclass
class AbsExpr(ASTNode):
    """abs(expr) - absolute value."""
    value: ASTNode = None

@dataclass
class MinExpr(ASTNode):
    """min(a, b) - minimum of two values."""
    left: ASTNode = None
    right: ASTNode = None

@dataclass
class MaxExpr(ASTNode):
    """max(a, b) - maximum of two values."""
    left: ASTNode = None
    right: ASTNode = None
