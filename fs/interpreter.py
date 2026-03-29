"""Tree-walking interpreter for the Fruit Salad language."""

from .ast_nodes import (
    Program, BlendDef, Block, PreserveDef, FreshDef, CandiedDef,
    Assignment, IfExpr, WhileExpr, EachExpr, LoopExpr,
    SnapStmt, SkipStmt, YieldStmt,
    BinaryExpr, UnaryExpr, CallExpr, IndexExpr, FieldExpr,
    NumberLiteral, StringLiteral, BoolLiteral, BasketLiteral, RangeLiteral,
    Identifier, StringInterpolation, DisplayExpr, ASTNode,
)
from .errors import SplatError, RotError


# --- Signal exceptions for control flow ---

class SnapSignal(Exception):
    """Raised by snap (break)."""
    pass


class SkipSignal(Exception):
    """Raised by skip (continue)."""
    pass


class YieldSignal(Exception):
    """Raised by yield (return)."""
    def __init__(self, value):
        self.value = value


# --- Environment ---

class Environment:
    def __init__(self, parent=None):
        self.parent = parent
        self.variables: dict[str, object] = {}
        self.immutables: set[str] = set()  # names that are preserve/candied

    def define(self, name: str, value: object, immutable: bool = False):
        self.variables[name] = value
        if immutable:
            self.immutables.add(name)

    def get(self, name: str, line: int = 0, col: int = 0) -> object:
        if name in self.variables:
            return self.variables[name]
        if self.parent is not None:
            return self.parent.get(name, line, col)
        raise SplatError(f"Undefined variable '{name}'", line, col)

    def set(self, name: str, value: object, line: int = 0, col: int = 0):
        if name in self.variables:
            if name in self.immutables:
                raise SplatError(f"Cannot reassign immutable variable '{name}'", line, col)
            self.variables[name] = value
            return
        if self.parent is not None:
            self.parent.set(name, value, line, col)
            return
        raise SplatError(f"Undefined variable '{name}'", line, col)


# --- Fruit Salad function wrapper ---

class FSFunction:
    def __init__(self, defn: BlendDef, closure: Environment):
        self.defn = defn
        self.closure = closure

    def __repr__(self):
        return f"<blend {self.defn.name}>"


# --- Interpreter ---

class Interpreter:
    def __init__(self, output_fn=None):
        self.globals = Environment()
        self.output_fn = output_fn or print  # for capturing output in tests

    def run(self, program: Program):
        # First pass: register all functions
        for fn in program.functions:
            self.globals.define(fn.name, FSFunction(fn, self.globals), immutable=True)

        # If there's a main function, call it
        if "main" in self.globals.variables:
            main_fn = self.globals.variables["main"]
            if isinstance(main_fn, FSFunction):
                self._call_function(main_fn, [], 0, 0)
                return

        # Otherwise execute top-level statements
        for stmt in program.statements:
            self._exec(stmt, self.globals)

    def _exec(self, node: ASTNode, env: Environment) -> object:
        """Execute a statement/expression and return its value."""
        method_name = f"_exec_{type(node).__name__}"
        method = getattr(self, method_name, None)
        if method is None:
            raise SplatError(f"Internal error: no handler for {type(node).__name__}", node.line, node.column)
        return method(node, env)

    def _exec_block(self, block: Block, env: Environment) -> object:
        """Execute a block, returning the value of the last expression."""
        result = None
        for stmt in block.statements:
            result = self._exec(stmt, env)
        return result

    # --- Declarations ---

    def _exec_PreserveDef(self, node: PreserveDef, env: Environment):
        value = self._exec(node.value, env)
        env.define(node.name, value, immutable=True)
        return value

    def _exec_FreshDef(self, node: FreshDef, env: Environment):
        value = self._exec(node.value, env)
        env.define(node.name, value, immutable=False)
        return value

    def _exec_CandiedDef(self, node: CandiedDef, env: Environment):
        value = self._exec(node.value, env)
        env.define(node.name, value, immutable=True)
        return value

    def _exec_Assignment(self, node: Assignment, env: Environment):
        value = self._exec(node.value, env)
        target = node.target

        if isinstance(target, Identifier):
            env.set(target.name, value, node.line, node.column)
        elif isinstance(target, IndexExpr):
            obj = self._exec(target.object, env)
            idx = self._exec(target.index, env)
            if isinstance(obj, list):
                if not isinstance(idx, int):
                    raise RotError("Basket index must be an Apple (integer)", node.line, node.column)
                if idx < 0 or idx >= len(obj):
                    raise SplatError(f"Basket index {idx} out of range (length {len(obj)})", node.line, node.column)
                obj[idx] = value
            else:
                raise RotError("Cannot index-assign to non-Basket value", node.line, node.column)
        else:
            raise SplatError("Invalid assignment target", node.line, node.column)
        return value

    # --- Control flow ---

    def _exec_IfExpr(self, node: IfExpr, env: Environment):
        cond = self._exec(node.condition, env)
        if self._truthy(cond):
            return self._exec_block(node.then_block, Environment(env))
        elif node.else_block is not None:
            if isinstance(node.else_block, Block):
                return self._exec_block(node.else_block, Environment(env))
            else:
                return self._exec(node.else_block, env)
        return None

    def _exec_WhileExpr(self, node: WhileExpr, env: Environment):
        result = None
        while self._truthy(self._exec(node.condition, env)):
            try:
                result = self._exec_block(node.body, Environment(env))
            except SnapSignal:
                break
            except SkipSignal:
                continue
        return result

    def _exec_EachExpr(self, node: EachExpr, env: Environment):
        iterable = self._exec(node.iterable, env)
        items = self._to_iterable(iterable, node)
        result = None
        for item in items:
            loop_env = Environment(env)
            loop_env.define(node.variable, item, immutable=False)
            try:
                result = self._exec_block(node.body, loop_env)
            except SnapSignal:
                break
            except SkipSignal:
                continue
        return result

    def _exec_LoopExpr(self, node: LoopExpr, env: Environment):
        result = None
        while True:
            try:
                result = self._exec_block(node.body, Environment(env))
            except SnapSignal:
                break
            except SkipSignal:
                continue
        return result

    def _exec_SnapStmt(self, node: SnapStmt, env: Environment):
        raise SnapSignal()

    def _exec_SkipStmt(self, node: SkipStmt, env: Environment):
        raise SkipSignal()

    def _exec_YieldStmt(self, node: YieldStmt, env: Environment):
        value = self._exec(node.value, env) if node.value else None
        raise YieldSignal(value)

    # --- Expressions ---

    def _exec_NumberLiteral(self, node: NumberLiteral, env: Environment):
        return node.value

    def _exec_StringLiteral(self, node: StringLiteral, env: Environment):
        return node.value

    def _exec_BoolLiteral(self, node: BoolLiteral, env: Environment):
        return node.value

    def _exec_Identifier(self, node: Identifier, env: Environment):
        return env.get(node.name, node.line, node.column)

    def _exec_BasketLiteral(self, node: BasketLiteral, env: Environment):
        return [self._exec(e, env) for e in node.elements]

    def _exec_RangeLiteral(self, node: RangeLiteral, env: Environment):
        start = self._exec(node.start, env)
        end = self._exec(node.end, env)
        if not isinstance(start, int) or not isinstance(end, int):
            raise RotError("Range bounds must be Apple (integer) values", node.line, node.column)
        if node.inclusive:
            return list(range(start, end + 1))
        return list(range(start, end))

    def _exec_BinaryExpr(self, node: BinaryExpr, env: Environment):
        left = self._exec(node.left, env)
        op = node.op

        # Short-circuit for logical operators
        if op == "&&":
            if not self._truthy(left):
                return left
            return self._exec(node.right, env)
        if op == "||":
            if self._truthy(left):
                return left
            return self._exec(node.right, env)

        right = self._exec(node.right, env)

        # Arithmetic with type coercion
        if op == "+":
            return self._add(left, right, node)
        if op == "-":
            return self._arithmetic(left, right, lambda a, b: a - b, "-", node)
        if op == "*":
            return self._arithmetic(left, right, lambda a, b: a * b, "*", node)
        if op == "/":
            if isinstance(right, (int, float)) and right == 0:
                raise SplatError("Division by zero", node.line, node.column)
            if isinstance(left, int) and isinstance(right, int):
                return left // right  # integer division for Apple / Apple
            return self._arithmetic(left, right, lambda a, b: a / b, "/", node)
        if op == "%":
            if isinstance(right, (int, float)) and right == 0:
                raise SplatError("Modulo by zero", node.line, node.column)
            return self._arithmetic(left, right, lambda a, b: a % b, "%", node)

        # Comparison
        if op == "==":
            return left == right
        if op == "!=":
            return left != right
        if op in ("<", ">", "<=", ">="):
            return self._compare(left, right, op, node)

        raise SplatError(f"Unknown operator '{op}'", node.line, node.column)

    def _exec_UnaryExpr(self, node: UnaryExpr, env: Environment):
        operand = self._exec(node.operand, env)
        if node.op == "-":
            if isinstance(operand, (int, float)):
                return -operand
            raise RotError("Unary '-' requires a numeric value", node.line, node.column)
        if node.op == "!":
            return not self._truthy(operand)
        raise SplatError(f"Unknown unary operator '{node.op}'", node.line, node.column)

    def _exec_CallExpr(self, node: CallExpr, env: Environment):
        callee = self._exec(node.callee, env)
        args = [self._exec(a, env) for a in node.args]

        if isinstance(callee, FSFunction):
            return self._call_function(callee, args, node.line, node.column)

        raise SplatError(f"Cannot call {self._type_name(callee)} value", node.line, node.column)

    def _exec_IndexExpr(self, node: IndexExpr, env: Environment):
        obj = self._exec(node.object, env)
        idx = self._exec(node.index, env)

        if isinstance(obj, list):
            if not isinstance(idx, int):
                raise RotError("Basket index must be an Apple (integer)", node.line, node.column)
            if idx < 0 or idx >= len(obj):
                raise SplatError(f"Basket index {idx} out of range (length {len(obj)})", node.line, node.column)
            return obj[idx]

        if isinstance(obj, str):
            if not isinstance(idx, int):
                raise RotError("Banana index must be an Apple (integer)", node.line, node.column)
            if idx < 0 or idx >= len(obj):
                raise SplatError(f"Banana index {idx} out of range (length {len(obj)})", node.line, node.column)
            return obj[idx]

        raise RotError(f"Cannot index into {self._type_name(obj)}", node.line, node.column)

    def _exec_FieldExpr(self, node: FieldExpr, env: Environment):
        obj = self._exec(node.object, env)
        field = node.field

        # Method calls: return a bound method tuple
        if isinstance(obj, (list, str)):
            if field == "len":
                return ("__method_len__", obj)
            if isinstance(obj, list):
                if field == "push":
                    return ("__method_push__", obj)
                if field == "pop":
                    return ("__method_pop__", obj)
                if field == "swap":
                    return ("__method_swap__", obj)

        raise SplatError(f"No field '{field}' on {self._type_name(obj)}", node.line, node.column)

    def _exec_DisplayExpr(self, node: DisplayExpr, env: Environment):
        values = [self._exec(a, env) for a in node.args]
        output_parts = [self._format_value(v) for v in values]
        self.output_fn(" ".join(output_parts))
        return None

    def _exec_StringInterpolation(self, node: StringInterpolation, env: Environment):
        parts = []
        for part in node.parts:
            if isinstance(part, str):
                parts.append(part)
            else:
                val = self._exec(part, env)
                parts.append(self._format_value(val))
        return "".join(parts)

    # --- Function calls ---

    def _call_function(self, fn: FSFunction, args: list, line: int, col: int) -> object:
        defn = fn.defn
        if len(args) != len(defn.params):
            raise SplatError(
                f"blend '{defn.name}' expects {len(defn.params)} argument(s), got {len(args)}",
                line, col,
            )

        call_env = Environment(fn.closure)
        for (param_name, _type_ann), arg_val in zip(defn.params, args):
            call_env.define(param_name, arg_val, immutable=False)

        try:
            result = self._exec_block(defn.body, call_env)
            return result
        except YieldSignal as ys:
            return ys.value

    # Overload CallExpr to handle method tuples
    def _exec_CallExpr(self, node: CallExpr, env: Environment):
        # Check if callee is a field expr for method call shorthand
        callee = self._exec(node.callee, env)
        args = [self._exec(a, env) for a in node.args]

        # Handle bound methods
        if isinstance(callee, tuple) and len(callee) == 2 and isinstance(callee[0], str):
            method_name, obj = callee
            if method_name == "__method_len__":
                if args:
                    raise SplatError("len() takes no arguments", node.line, node.column)
                return len(obj)
            if method_name == "__method_push__":
                if len(args) != 1:
                    raise SplatError("push() takes exactly 1 argument", node.line, node.column)
                obj.append(args[0])
                return None
            if method_name == "__method_pop__":
                if args:
                    raise SplatError("pop() takes no arguments", node.line, node.column)
                if len(obj) == 0:
                    raise SplatError("Cannot pop from empty Basket", node.line, node.column)
                return obj.pop()
            if method_name == "__method_swap__":
                if len(args) != 2:
                    raise SplatError("swap() takes exactly 2 arguments", node.line, node.column)
                i, j = args
                if not isinstance(i, int) or not isinstance(j, int):
                    raise RotError("swap() indices must be Apple (integer)", node.line, node.column)
                if i < 0 or i >= len(obj) or j < 0 or j >= len(obj):
                    raise SplatError("swap() index out of range", node.line, node.column)
                obj[i], obj[j] = obj[j], obj[i]
                return None

        if isinstance(callee, FSFunction):
            return self._call_function(callee, args, node.line, node.column)

        raise SplatError(f"Cannot call {self._type_name(callee)} value", node.line, node.column)

    # --- Helper methods ---

    def _truthy(self, value: object) -> bool:
        if value is None:
            return False
        if isinstance(value, bool):
            return value
        if isinstance(value, int):
            return value != 0
        if isinstance(value, float):
            return value != 0.0
        if isinstance(value, str):
            return len(value) > 0
        if isinstance(value, list):
            return len(value) > 0
        return True

    def _add(self, left, right, node):
        # String concatenation: anything + Banana = Banana
        if isinstance(left, str) or isinstance(right, str):
            return self._format_value(left) + self._format_value(right)
        if isinstance(left, (int, float)) and isinstance(right, (int, float)):
            # Apple + Date = Date
            if isinstance(left, float) or isinstance(right, float):
                return float(left) + float(right)
            return left + right
        if isinstance(left, list) and isinstance(right, list):
            return left + right
        raise RotError(
            f"Cannot add {self._type_name(left)} and {self._type_name(right)}",
            node.line, node.column,
        )

    def _arithmetic(self, left, right, op_fn, op_str, node):
        if not isinstance(left, (int, float)) or not isinstance(right, (int, float)):
            raise RotError(
                f"Cannot use '{op_str}' on {self._type_name(left)} and {self._type_name(right)}",
                node.line, node.column,
            )
        if isinstance(left, float) or isinstance(right, float):
            return op_fn(float(left), float(right))
        return op_fn(left, right)

    def _compare(self, left, right, op, node):
        if isinstance(left, (int, float)) and isinstance(right, (int, float)):
            if op == "<":
                return left < right
            if op == ">":
                return left > right
            if op == "<=":
                return left <= right
            if op == ">=":
                return left >= right
        if isinstance(left, str) and isinstance(right, str):
            if op == "<":
                return left < right
            if op == ">":
                return left > right
            if op == "<=":
                return left <= right
            if op == ">=":
                return left >= right
        raise RotError(
            f"Cannot compare {self._type_name(left)} and {self._type_name(right)} with '{op}'",
            node.line, node.column,
        )

    def _to_iterable(self, value, node):
        if isinstance(value, list):
            return value
        if isinstance(value, str):
            return list(value)
        raise RotError(f"Cannot iterate over {self._type_name(value)}", node.line, node.column)

    def _format_value(self, value: object) -> str:
        if value is None:
            return "pit"
        if isinstance(value, bool):
            return "true" if value else "false"
        if isinstance(value, int):
            return str(value)
        if isinstance(value, float):
            # Format cleanly: remove trailing zeros but keep at least one decimal
            s = f"{value:.10g}"
            return s
        if isinstance(value, str):
            return value
        if isinstance(value, list):
            inner = ", ".join(self._format_value(v) for v in value)
            return f"[{inner}]"
        if isinstance(value, FSFunction):
            return repr(value)
        return str(value)

    def _type_name(self, value: object) -> str:
        if value is None:
            return "Pit"
        if isinstance(value, bool):
            return "Cherry"
        if isinstance(value, int):
            return "Apple"
        if isinstance(value, float):
            return "Date"
        if isinstance(value, str):
            return "Banana"
        if isinstance(value, list):
            return "Basket"
        if isinstance(value, FSFunction):
            return "Blend"
        return type(value).__name__
