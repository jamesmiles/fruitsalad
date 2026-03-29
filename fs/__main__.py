"""Entry point for the Fruit Salad interpreter: python -m fs <filename.fs>"""

import sys

from .lexer import Lexer
from .parser import Parser
from .interpreter import Interpreter
from .errors import LexError, ParseError, SplatError, RotError, FruitSaladError


def main():
    if len(sys.argv) < 2:
        print("Usage: python -m fs <filename.fs>", file=sys.stderr)
        sys.exit(1)

    filename = sys.argv[1]

    try:
        with open(filename, "r") as f:
            source = f.read()
    except FileNotFoundError:
        print(f"Error: File not found: {filename}", file=sys.stderr)
        sys.exit(1)
    except IOError as e:
        print(f"Error: Could not read file: {e}", file=sys.stderr)
        sys.exit(1)

    # Phase 1: Lex
    try:
        lexer = Lexer(source, filename)
        tokens = lexer.tokenize()
    except LexError as e:
        print(f"{filename}: {e}", file=sys.stderr)
        sys.exit(1)

    # Phase 2: Parse
    try:
        parser = Parser(tokens)
        program = parser.parse()
    except ParseError as e:
        print(f"{filename}: {e}", file=sys.stderr)
        sys.exit(1)

    # Phase 3: Interpret
    try:
        interpreter = Interpreter()
        interpreter.run(program)
    except (SplatError, RotError) as e:
        print(f"{filename}: {e}", file=sys.stderr)
        sys.exit(2)
    except FruitSaladError as e:
        print(f"{filename}: {e}", file=sys.stderr)
        sys.exit(2)


if __name__ == "__main__":
    main()
