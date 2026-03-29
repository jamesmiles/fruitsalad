"""Tree-walking interpreter for the Fruit Salad language."""

from .ast_nodes import (
    Program, BlendDef, Block, PreserveDef, FreshDef, CandiedDef,
    Assignment, IfExpr, WhileExpr, EachExpr, LoopExpr,
    SnapStmt, SkipStmt, YieldStmt,
    BinaryExpr, UnaryExpr, CallExpr, IndexExpr, FieldExpr,
    NumberLiteral, StringLiteral, BoolLiteral, BasketLiteral, RangeLiteral,
    Identifier, StringInterpolation, DisplayExpr, ASTNode,
    BowlDef, BowlLiteral, MedleyDef, MedleyVariantExpr, SortExpr,
    SqueezeLiteral, PantryLiteral,
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


# --- Phase 2 runtime types ---

class FSBowlType:
    """Bowl (struct) type definition."""
    def __init__(self, name: str, fields: list):
        self.name = name
        self.fields = fields  # list of (name, type_annotation?)

    def __repr__(self):
        return f"<bowl {self.name}>"


class FSBowlInstance:
    """Runtime bowl instance."""
    def __init__(self, bowl_type: FSBowlType, field_values: dict):
        self.bowl_type = bowl_type
        self.field_values = field_values  # dict of field_name -> value

    def __repr__(self):
        fields = ", ".join(f"{k}: {v!r}" for k, v in self.field_values.items())
        return f"{self.bowl_type.name} {{ {fields} }}"


class FSMedleyType:
    """Medley (enum) type definition."""
    def __init__(self, name: str, variants: list):
        self.name = name
        self.variants = variants  # list of (variant_name, fields_or_None)

    def __repr__(self):
        return f"<medley {self.name}>"


class FSMedleyVariant:
    """Runtime medley variant instance."""
    def __init__(self, medley_name: str, variant_name: str, values: list):
        self.medley_name = medley_name
        self.variant_name = variant_name
        self.values = values  # list of values

    def __repr__(self):
        if self.values:
            args = ", ".join(repr(v) for v in self.values)
            return f"{self.medley_name}.{self.variant_name}({args})"
        return f"{self.medley_name}.{self.variant_name}"


class FSClosure:
    """A closure (squeeze) - lambda with captured environment."""
    def __init__(self, params: list, body, closure: 'Environment'):
        self.params = params  # list of (name, type_ann?)
        self.body = body
        self.closure = closure

    def __repr__(self):
        return f"<squeeze>"


# --- Interpreter ---

class Interpreter:
    def __init__(self, output_fn=None):
        self.globals = Environment()
        self.output_fn = output_fn or print  # for capturing output in tests

    def run(self, program: Program):
        # First pass: register all functions
        for fn in program.functions:
            self.globals.define(fn.name, FSFunction(fn, self.globals), immutable=True)

        # Second pass: execute top-level statements (Bowl/Medley defs, constants, etc.)
        for stmt in program.statements:
            self._exec(stmt, self.globals)

        # If there's a main function, call it
        if "main" in self.globals.variables:
            main_fn = self.globals.variables["main"]
            if isinstance(main_fn, FSFunction):
                self._call_function(main_fn, [], 0, 0)

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
        elif isinstance(target, FieldExpr):
            obj = self._exec(target.object, env)
            if isinstance(obj, FSBowlInstance):
                if target.field in obj.field_values:
                    obj.field_values[target.field] = value
                else:
                    raise SplatError(f"No field '{target.field}' on bowl '{obj.bowl_type.name}'", node.line, node.column)
            else:
                raise RotError("Cannot field-assign to non-Bowl value", node.line, node.column)
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

        # Bowl instance field access
        if isinstance(obj, FSBowlInstance):
            if field in obj.field_values:
                return obj.field_values[field]
            raise SplatError(f"No field '{field}' on bowl '{obj.bowl_type.name}'", node.line, node.column)

        # Medley type: return variant constructor or unit variant
        if isinstance(obj, FSMedleyType):
            # Find the variant
            for vname, vfields in obj.variants:
                if vname == field:
                    if vfields is None or len(vfields) == 0:
                        # Unit variant - return the variant directly
                        return FSMedleyVariant(obj.name, field, [])
                    else:
                        # Variant with fields - return a constructor closure
                        medley_name = obj.name
                        variant_name = field
                        expected_count = len(vfields)
                        class _VariantConstructor:
                            def __init__(self, mn, vn, cnt):
                                self.medley_name = mn
                                self.variant_name = vn
                                self.expected_count = cnt
                            def __repr__(self):
                                return f"<{self.medley_name}.{self.variant_name} constructor>"
                        return _VariantConstructor(medley_name, variant_name, expected_count)
            raise SplatError(f"No variant '{field}' on medley '{obj.name}'", node.line, node.column)

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
                if field == "copy":
                    return ("__method_copy__", obj)
                if field == "contains":
                    return ("__method_contains__", obj)
                if field == "remove":
                    return ("__method_remove__", obj)
                if field == "slice":
                    return ("__method_slice__", obj)

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

    # --- Phase 2: Bowl, Medley, Sort, Squeeze ---

    def _exec_BowlDef(self, node: BowlDef, env: Environment):
        bowl_type = FSBowlType(node.name, node.fields)
        env.define(node.name, bowl_type, immutable=True)
        return bowl_type

    def _exec_BowlLiteral(self, node: BowlLiteral, env: Environment):
        bowl_type = env.get(node.name, node.line, node.column)
        if not isinstance(bowl_type, FSBowlType):
            raise SplatError(f"'{node.name}' is not a Bowl type", node.line, node.column)
        field_values = {}
        for fname, fexpr in node.field_values:
            field_values[fname] = self._exec(fexpr, env)
        # Validate field names
        defined_fields = {f[0] for f in bowl_type.fields}
        for fname in field_values:
            if fname not in defined_fields:
                raise SplatError(f"Unknown field '{fname}' for bowl '{node.name}'", node.line, node.column)
        return FSBowlInstance(bowl_type, field_values)

    def _exec_MedleyDef(self, node: MedleyDef, env: Environment):
        medley_type = FSMedleyType(node.name, node.variants)
        env.define(node.name, medley_type, immutable=True)
        return medley_type

    def _exec_MedleyVariantExpr(self, node: MedleyVariantExpr, env: Environment):
        args = [self._exec(a, env) for a in node.args]
        return FSMedleyVariant(node.medley_name, node.variant_name, args)

    def _exec_SortExpr(self, node: SortExpr, env: Environment):
        subject = self._exec(node.subject, env)
        for pattern, guard, body in node.arms:
            bindings = {}
            if self._match_pattern(pattern, subject, bindings):
                # Check guard if present
                if guard is not None:
                    guard_env = Environment(env)
                    for k, v in bindings.items():
                        guard_env.define(k, v, immutable=False)
                    if not self._truthy(self._exec(guard, guard_env)):
                        continue
                # Execute body
                arm_env = Environment(env)
                for k, v in bindings.items():
                    arm_env.define(k, v, immutable=False)
                if isinstance(body, Block):
                    return self._exec_block(body, arm_env)
                else:
                    return self._exec(body, arm_env)
        raise SplatError("non-exhaustive sort", node.line, node.column)

    def _match_pattern(self, pattern, subject, bindings: dict) -> bool:
        """Try to match a pattern against a subject value.
        If successful, populate bindings dict and return True."""
        kind = pattern[0]

        if kind == "wildcard":
            return True

        if kind == "literal":
            return subject == pattern[1]

        if kind == "binding":
            name = pattern[1]
            bindings[name] = subject
            return True

        if kind == "variant":
            _, medley_name, variant_name, binding_names = pattern
            if not isinstance(subject, FSMedleyVariant):
                return False
            if subject.variant_name != variant_name:
                return False
            # Optionally check medley_name matches
            if subject.medley_name != medley_name:
                return False
            if binding_names:
                if len(binding_names) != len(subject.values):
                    return False
                for bname, val in zip(binding_names, subject.values):
                    if bname != "_":
                        bindings[bname] = val
            return True

        return False

    def _exec_SqueezeLiteral(self, node: SqueezeLiteral, env: Environment):
        return FSClosure(node.params, node.body, env)

    def _exec_PantryLiteral(self, node: PantryLiteral, env: Environment):
        entries = {}
        for key_expr, val_expr in node.entries:
            key = self._exec(key_expr, env)
            val = self._exec(val_expr, env)
            entries[key] = val
        return entries

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

    def _call_closure(self, closure: FSClosure, args: list, line: int, col: int) -> object:
        if len(args) != len(closure.params):
            raise SplatError(
                f"squeeze expects {len(closure.params)} argument(s), got {len(args)}",
                line, col,
            )
        call_env = Environment(closure.closure)
        for (param_name, _type_ann), arg_val in zip(closure.params, args):
            call_env.define(param_name, arg_val, immutable=False)

        try:
            if isinstance(closure.body, Block):
                return self._exec_block(closure.body, call_env)
            else:
                return self._exec(closure.body, call_env)
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
            if method_name == "__method_copy__":
                if args:
                    raise SplatError("copy() takes no arguments", node.line, node.column)
                return list(obj)
            if method_name == "__method_contains__":
                if len(args) != 1:
                    raise SplatError("contains() takes exactly 1 argument", node.line, node.column)
                return args[0] in obj
            if method_name == "__method_remove__":
                if len(args) != 1:
                    raise SplatError("remove() takes exactly 1 argument", node.line, node.column)
                idx = args[0]
                if not isinstance(idx, int):
                    raise RotError("remove() index must be an Apple (integer)", node.line, node.column)
                if idx < 0 or idx >= len(obj):
                    raise SplatError(f"remove() index {idx} out of range (length {len(obj)})", node.line, node.column)
                return obj.pop(idx)
            if method_name == "__method_slice__":
                if len(args) != 2:
                    raise SplatError("slice() takes exactly 2 arguments", node.line, node.column)
                start, end = args
                if not isinstance(start, int) or not isinstance(end, int):
                    raise RotError("slice() arguments must be Apple (integer)", node.line, node.column)
                return obj[start:end]

        if isinstance(callee, FSFunction):
            return self._call_function(callee, args, node.line, node.column)

        # Handle closures (squeeze)
        if isinstance(callee, FSClosure):
            return self._call_closure(callee, args, node.line, node.column)

        # Handle variant constructors
        if hasattr(callee, 'medley_name') and hasattr(callee, 'variant_name') and hasattr(callee, 'expected_count'):
            if len(args) != callee.expected_count:
                raise SplatError(
                    f"Variant '{callee.medley_name}.{callee.variant_name}' expects {callee.expected_count} argument(s), got {len(args)}",
                    node.line, node.column,
                )
            return FSMedleyVariant(callee.medley_name, callee.variant_name, args)

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
        if isinstance(value, FSBowlInstance):
            fields = ", ".join(f"{k}: {self._format_value(v)}" for k, v in value.field_values.items())
            return f"{value.bowl_type.name} {{ {fields} }}"
        if isinstance(value, FSMedleyVariant):
            if value.values:
                args = ", ".join(self._format_value(v) for v in value.values)
                return f"{value.medley_name}.{value.variant_name}({args})"
            return f"{value.medley_name}.{value.variant_name}"
        if isinstance(value, FSBowlType):
            return repr(value)
        if isinstance(value, FSMedleyType):
            return repr(value)
        if isinstance(value, FSClosure):
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
        if isinstance(value, FSBowlInstance):
            return f"Bowl({value.bowl_type.name})"
        if isinstance(value, FSMedleyVariant):
            return f"Medley({value.medley_name})"
        if isinstance(value, FSBowlType):
            return "BowlType"
        if isinstance(value, FSMedleyType):
            return "MedleyType"
        if isinstance(value, FSClosure):
            return "Squeeze"
        return type(value).__name__
