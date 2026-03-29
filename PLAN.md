# Fruit Salad Language - Master Plan

## Table of Contents
1. [Language Standard](#1-language-standard)
2. [Execution Harnesses](#2-execution-harnesses)
3. [Test Strategy](#3-test-strategy)
4. [Development Process](#4-development-process)
5. [Milestones](#5-milestones)

---

## 1. Language Standard

### 1.1 Philosophy

Fruit Salad is a memory-safe, expression-oriented, statically-typed language with fruit-themed syntax. It prioritises clarity, safety, and developer ergonomics while introducing genuinely innovative features around data provenance tracking, pipeline composition, and structured concurrency.

**Core principles:**
- Everything is an expression (no statements, everything yields a value)
- Immutable by default (`preserve` semantics)
- Memory safe via automatic composting (deterministic resource cleanup + GC)
- No null — use `Ripe<T>` (option type) instead
- Errors as values, not exceptions — `Harvest<T, Rot>` (result type)
- Structural typing with nominal opt-in

### 1.2 Fruit-Themed Glossary

| Concept | Fruit Salad Term | Rationale |
|---------|-----------------|-----------|
| Primitive types | **Fruits** | The basic ingredients |
| Integer | **Apple** | The most common fruit |
| Float | **Date** | Dates have decimal-like seeds |
| String | **Banana** | Long and sequential |
| Boolean | **Cherry** | Small, binary (pit or no pit) |
| Byte | **Grape** | Small, comes in bunches |
| Struct/Record | **Bowl** | A bowl holds a mix of fruits |
| Enum/Union | **Medley** | A medley is one-of-many |
| Array/List | **Basket\<T\>** | A basket of same fruits |
| Map/Dict | **Pantry\<K,V\>** | Organised storage |
| Set | **Punnet\<T\>** | Unique items in a container |
| Tuple | **Skewer\<A,B,...\>** | Fruits on a stick, ordered |
| Function | **Blend** | Blending ingredients together |
| Method | **Prep** | Preparing a specific bowl |
| Interface/Trait | **Recipe** | A recipe describes what to make |
| Module/Namespace | **Orchard** | Where fruits grow |
| Package | **Grove** | Collection of orchards |
| Import | **Pick** | Pick fruit from an orchard |
| Generic type param | **Seed\<T\>** | Seeds determine what grows |
| Null/None | **Pit** | The empty core |
| Option type | **Ripe\<T\>** | Maybe ripe, maybe not |
| Result type | **Harvest\<T, Rot\>** | Either good fruit or rot |
| Error type | **Rot** | When fruit goes bad |
| Exception (panic) | **Splat** | Fruit hits the floor |
| Pattern matching | **Sort** | Sorting fruit by type |
| Unwrap/Extract | **Juice** | Juicing extracts the good stuff |
| Type introspection | **Peel** | Peeling back layers to see inside |
| Iterator/Generator | **Vine** | Grows values one at a time |
| Async operation | **Ferment** | Takes time, produces results |
| Future/Promise | **Cask\<T\>** | Fermenting value, not ready yet |
| Mutex/Lock | **Jam** | Preserved, controlled access |
| Channel | **Chute** | Fruit slides down a chute |
| Immutable binding | **preserve** | Preserved fruit doesn't change |
| Mutable binding | **fresh** | Fresh fruit can still change |
| Constant | **candied** | Candied at compile time |
| Decorator/Annotation | **Garnish** | Adds flavour on top |
| Macro | **Zest** | Adds programmatic flavour |
| Constructor | **Sprout** | How a fruit begins |
| Destructor/Cleanup | **Compost** | Returns to the earth |
| Scope/Lifetime | **Season** | Everything has its season |
| Pipeline operator | **~\>** | The **smoothie** operator |
| Lambda/Closure | **Squeeze** | A quick squeeze of juice |
| Test | **Taste** | Taste-testing the output |
| Assertion | **Sniff** | Sniff-test for correctness |
| Print/Debug | **Display** | Show on the fruit stand |
| Comment | **-- or -{ }-** | Dashes, like cutting |
| Main entry point | **blend main()** | The main blend |

### 1.3 Innovative Language Features

#### 1.3.1 Freshness Tracking (Data Provenance)

Every value in Fruit Salad carries an invisible **freshness tag** that tracks its provenance. Values from untrusted sources (user input, network, files) start as `wilted` and must be explicitly `washed` (validated/sanitised) before being used in sensitive operations.

```
-- Values from external sources are automatically tagged `wilted`
fresh input = pick_from_stdin()  -- wilted Banana

-- Cannot use wilted values in sensitive operations
serve_query(input)  -- COMPILE ERROR: cannot serve wilted Banana

-- Must wash first (validate/sanitise)
fresh clean_input = wash(input, |s| s ~> trim ~> escape_sql)
serve_query(clean_input)  -- OK: washed Banana
```

This is compile-time taint tracking built into the type system — no runtime overhead.

#### 1.3.2 Smoothie Operator (~>)

Pipeline operator that chains operations left-to-right, with the previous result fed as the first argument:

```
fresh result = basket ~> filter(is_ripe) ~> map(juice) ~> reduce(mix, empty_glass)
```

#### 1.3.3 Vine Iterators with Backpressure

Vines are lazy generators that support built-in backpressure signalling:

```
blend fibonacci() -> Vine<Apple> {
    fresh a = 0
    fresh b = 1
    vine {
        yield a
        preserve temp = a
        a = b
        b = temp + b
    }
}

-- Consumer controls pace
fibonacci() ~> take(10) ~> each(display)
```

#### 1.3.4 Sort Expressions (Pattern Matching)

Exhaustive pattern matching with destructuring:

```
blend describe(f: Ripe<Apple>) -> Banana {
    sort f {
        ripe(n) where n > 100 => "big number: {n}"
        ripe(n)               => "number: {n}"
        pit                   => "nothing here"
    }
}
```

#### 1.3.5 Fermentation (Structured Concurrency)

Async operations use a fermentation metaphor with structured concurrency guarantees:

```
blend fetch_all(urls: Basket<Banana>) -> Basket<Harvest<Banana, Rot>> {
    ferment barrel {
        urls ~> each(|url| {
            barrel.add(ferment fetch(url))
        })
    }
    -- All casks are resolved when barrel completes
    -- If any splat, all are cancelled
}
```

#### 1.3.6 Composting (Deterministic Cleanup)

Resources implement the `Compostable` recipe for deterministic cleanup:

```
blend read_file(path: Banana) -> Harvest<Banana, Rot> {
    fresh file = compost open(path)?  -- auto-composts when scope ends
    file.read_all()
}
```

#### 1.3.7 Juicing (Value Extraction)

`juice` is a unified extraction operator that works on `Ripe<T>`, `Harvest<T,E>`, and custom types:

```
fresh val = maybe_value.juice          -- splats if pit (like unwrap)
fresh val = maybe_value.juice_or(42)   -- default if pit
fresh val = result.juice               -- splats if rot
fresh val = result.juice_or_rot        -- propagates rot (like ?)
```

The `?` suffix is syntactic sugar for `.juice_or_rot`:

```
blend safe_divide(a: Apple, b: Apple) -> Harvest<Apple, Rot> {
    if b == 0 {
        toss rot("division by zero")
    }
    ripe(a / b)
}

blend calc() -> Harvest<Apple, Rot> {
    fresh x = safe_divide(10, 2)?  -- propagates rot if any
    fresh y = safe_divide(x, 3)?
    ripe(y)
}
```

#### 1.3.8 Peeling (Type Introspection)

`peel` allows runtime type inspection and is also used for generic constraints:

```
blend describe_type<T>(val: T) -> Banana {
    peel T {
        Apple   => "it's an integer"
        Banana  => "it's a string"
        Bowl    => "it's a struct with fields: {T.fields}"
        _       => "something else"
    }
}
```

### 1.4 Syntax Overview

```
-- This is a line comment
-{ This is a
   block comment }-

-- Import from an orchard
pick io from std
pick math from std

-- Constants
candied PI = 3.14159265358979

-- Bowl (struct) definition
bowl Point {
    x: Date,
    y: Date,
}

-- Recipe (interface/trait)
recipe Describable {
    prep describe(self) -> Banana
}

-- Implement recipe for bowl
prep Point as Describable {
    blend describe(self) -> Banana {
        "({self.x}, {self.y})"
    }
}

-- Medley (enum)
medley Shape {
    Circle(radius: Date),
    Rectangle(width: Date, height: Date),
    Triangle(base: Date, height: Date),
}

-- Functions
blend area(s: Shape) -> Date {
    sort s {
        Shape.Circle(r)    => PI * r * r
        Shape.Rectangle(w, h) => w * h
        Shape.Triangle(b, h)  => 0.5 * b * h
    }
}

-- Entry point
blend main() {
    preserve shapes = [
        Shape.Circle(radius: 5.0),
        Shape.Rectangle(width: 3.0, height: 4.0),
    ]

    shapes ~> each(|s| {
        display("Area: {area(s)}")
    })
}
```

### 1.5 Type System

- **Primitives:** Apple (i64), Date (f64), Banana (UTF-8 string), Cherry (bool), Grape (u8)
- **Sized integers:** Apple8, Apple16, Apple32, Apple64 (signed), Grape16, Grape32, Grape64 (unsigned)
- **Collections:** Basket\<T\>, Pantry\<K,V\>, Punnet\<T\>, Skewer\<A,B,...\>
- **Algebraic types:** Bowl (product), Medley (sum)
- **Special:** Ripe\<T\> (option), Harvest\<T,Rot\> (result), Vine\<T\> (iterator), Cask\<T\> (future)
- **Freshness modifiers:** `wilted T`, `washed T` (compile-time taint tracking)
- **Nothing type:** `Void` (unit/void, the empty bowl)

### 1.6 Operator Precedence (high to low)

1. `.` (field access), `()` (call), `[]` (index)
2. `juice`, `?` (extraction)
3. `-` (unary negate), `!` (not), `peel` (type introspection)
4. `*`, `/`, `%` (multiplicative)
5. `+`, `-` (additive)
6. `~>` (smoothie/pipeline)
7. `<`, `>`, `<=`, `>=` (comparison)
8. `==`, `!=` (equality)
9. `&&` (logical and)
10. `||` (logical or)
11. `=` (assignment)

### 1.7 Control Flow

```
-- If expression
fresh x = if condition { a } else { b }

-- Sort (match) expression
fresh y = sort value {
    pattern1 => result1
    pattern2 => result2
}

-- Loops
fresh sum = 0
each item in basket {
    sum = sum + item
}

-- While
while condition {
    -- body
}

-- Loop (infinite, break with 'snap')
loop {
    if done { snap }
}

-- Range
each i in 0..10 {
    display(i)
}
```

### 1.8 Error Handling

```
-- Functions that can fail return Harvest<T, Rot>
blend parse_apple(s: Banana) -> Harvest<Apple, Rot> {
    -- ...
    toss rot("not a number: {s}")
}

-- Propagate with ?
blend double_parse(s: Banana) -> Harvest<Apple, Rot> {
    fresh n = parse_apple(s)?
    ripe(n * 2)
}

-- Handle errors with sort
sort double_parse("42") {
    ripe(n) => display("Got: {n}")
    rot(e)  => display("Error: {e}")
}

-- Unrecoverable: splat
blend must_have(val: Ripe<Apple>) -> Apple {
    val.juice  -- splats if pit, crashing the program
}
```

### 1.9 Memory Model

Fruit Salad uses **automatic composting**: a combination of reference counting with cycle detection. All values are heap-allocated with small-value optimisation (primitives and small bowls stay on the stack).

- No manual memory management
- No dangling references (borrow checker lite: single owner with shared immutable refs)
- Deterministic cleanup via `compost` blocks
- No data races: `fresh` bindings are thread-local; sharing requires `Jam<T>` or message passing via `Chute<T>`

---

## 2. Execution Harnesses

### 2.1 Architecture Overview

```
                    +-------------------+
                    |   Test Runner     |
                    |   (Python/Make)   |
                    +---+----------+----+
                        |          |
              +---------+          +----------+
              v                               v
    +------------------+            +------------------+
    |  Hosted Harness  |            | Freestanding     |
    |  (Docker)        |            | Harness (QEMU)   |
    +------------------+            +------------------+
    | - fs interpreter |            | - Minimal OS     |
    | - Resource limits|            | - fs interpreter |
    | - Output capture |            | - Serial output  |
    | - Timeout guard  |            | - Memory dumps   |
    +------------------+            +------------------+
```

### 2.2 Hosted Harness (Docker)

**Purpose:** Safe execution of Fruit Salad programs in a sandboxed Linux environment with resource limits, output capture, and timeout enforcement.

**Implementation:**

```dockerfile
# harness/hosted/Dockerfile
FROM python:3.12-slim
COPY fs /usr/local/bin/fs
COPY harness/hosted/runner.py /runner.py
ENTRYPOINT ["python3", "/runner.py"]
```

**Runner contract:**
- Accepts a `.fs` file via stdin or mounted volume
- Executes with configurable timeout (default: 30s)
- Captures stdout, stderr, exit code
- Captures memory usage via `/proc` monitoring
- Produces JSON output:
```json
{
    "exit_code": 0,
    "stdout": "...",
    "stderr": "...",
    "duration_ms": 142,
    "peak_memory_bytes": 4194304,
    "timeout": false,
    "signals": []
}
```

**Resource limits:**
- Memory: 256MB (configurable)
- CPU time: 30s (configurable)
- No network access
- Read-only filesystem (except /tmp)
- No privilege escalation

### 2.3 Freestanding Harness (QEMU)

**Purpose:** Execute Fruit Salad programs in a bare-metal-like environment to verify the language works without OS dependencies — targeting embedded/systems use cases.

**Implementation:**
- QEMU system emulation (x86_64 or aarch64)
- Minimal bootloader that initialises serial output
- fs interpreter compiled as a freestanding binary (no libc)
- Output captured via QEMU serial port redirection
- Memory dumps via QEMU monitor protocol

**Runner contract:**
- Same JSON output format as hosted harness
- Additional field: `memory_dump` (base64-encoded if requested)
- Timeout via QEMU watchdog timer

**Note:** The freestanding harness is a stretch goal. We start with hosted (Docker) and local execution, adding QEMU support once the interpreter is stable.

### 2.4 Local Harness

**Purpose:** Fast local execution for development iteration — no Docker overhead.

**Implementation:**
- Direct execution of `fs` interpreter
- Python subprocess wrapper with timeout and output capture
- Same JSON output contract
- Used for the tight development feedback loop
- Falls back to Docker harness for CI or when `--sandboxed` flag is set

---

## 3. Test Strategy

### 3.1 Problem Library

A curated catalogue of algorithms and problems, organised by category and difficulty. Each entry is a problem specification (not a solution) that we must implement in Fruit Salad.

#### Categories and Sources

| Category | Problems | Primary Sources |
|----------|----------|-----------------|
| **Sorting** | Bubble, Selection, Insertion, Merge, Quick, Heap, Radix, Counting | TheAlgorithms, Rosetta Code |
| **Searching** | Linear, Binary, DFS, BFS, Interpolation | TheAlgorithms, Rosetta Code |
| **Data Structures** | Stack, Queue, Linked List, BST, Hash Map, Heap, Trie, Graph | TheAlgorithms, Stony Brook |
| **Math** | GCD, LCM, Fibonacci, Primes (Sieve), Factorial, Power, Modular Arithmetic | Rosetta Code, LeetCode |
| **String** | Reverse, Palindrome, Anagram, Pattern Match, Levenshtein Distance | LeetCode, Rosetta Code |
| **Dynamic Programming** | Fibonacci, Knapsack, LCS, Edit Distance, Coin Change, Matrix Chain | LeetCode, Stony Brook |
| **Graph** | Dijkstra, Bellman-Ford, Floyd-Warshall, Kruskal, Prim, Topological Sort | TheAlgorithms, Stony Brook |
| **Classic Problems** | FizzBuzz, Two Sum, Towers of Hanoi, N-Queens, Conway's Game of Life | LeetCode, Rosetta Code |
| **Concurrency** | Producer-Consumer, Dining Philosophers, Parallel Map-Reduce | CPython tests, Rosetta Code |
| **I/O & Parsing** | Read file, CSV parse, JSON parse, Command line args | CPython/libc++ tests |
| **Error Handling** | Division by zero, Stack overflow, Invalid input, Resource exhaustion | Custom |
| **Type System** | Generics, Trait dispatch, Type inference, Freshness tracking | Custom |

#### Problem Library Format

Each problem is stored in `problems/` as a TOML file:

```toml
# problems/sorting/bubble_sort.toml
[problem]
id = "sort-001"
name = "Bubble Sort"
category = "sorting"
difficulty = "easy"
source = "TheAlgorithms"
description = "Implement bubble sort algorithm"

[[test_cases]]
input = "[5, 3, 8, 1, 2]"
expected_output = "[1, 2, 3, 5, 8]"

[[test_cases]]
input = "[]"
expected_output = "[]"

[[test_cases]]
input = "[1]"
expected_output = "[1]"

[[test_cases]]
input = "[3, 3, 3]"
expected_output = "[3, 3, 3]"
```

### 3.2 Test Types

#### 3.2.1 Positive Tests (Solutions)
Each problem solution in `tests/solutions/` is a `.fs` file with inline expected-output annotations:

```
-- @test sort-001: Bubble Sort
-- @input [5, 3, 8, 1, 2]
-- @expect [1, 2, 3, 5, 8]

blend bubble_sort(items: Basket<Apple>) -> Basket<Apple> {
    fresh arr = items.copy()
    fresh n = arr.len()
    each i in 0..n {
        each j in 0..(n - i - 1) {
            if arr[j] > arr[j + 1] {
                arr.swap(j, j + 1)
            }
        }
    }
    arr
}

blend main() {
    preserve input = [5, 3, 8, 1, 2]
    preserve sorted = bubble_sort(input)
    display(sorted)
}
```

#### 3.2.2 Negative Tests (Expected Failures)
Programs in `tests/negative/` that must fail to compile or produce specific runtime errors:

```
-- @test neg-type-001: Type mismatch
-- @expect_error "type mismatch: expected Apple, got Banana"

blend main() {
    preserve x: Apple = "hello"  -- should fail: Banana assigned to Apple
}
```

```
-- @test neg-fresh-001: Freshness violation
-- @expect_error "cannot use wilted Banana in washed context"

pick io from std

blend serve(query: washed Banana) {
    display(query)
}

blend main() {
    fresh input = io.read_line()  -- wilted
    serve(input)                  -- error: wilted passed to washed
}
```

#### 3.2.3 Negative Runtime Tests
Programs that should compile but produce runtime errors:

```
-- @test neg-rt-001: Division by zero
-- @expect_runtime_error "division by zero"

blend main() {
    fresh x = 10 / 0
}
```

### 3.3 Test Runner

A Python-based test runner that:

1. Discovers all test files in `tests/`
2. Parses inline annotations (`@test`, `@input`, `@expect`, `@expect_error`, `@expect_runtime_error`)
3. Executes each test via the selected harness (local/Docker/QEMU)
4. Compares actual vs expected output
5. Produces a JSON report and human-readable summary
6. Calculates reward signal: `passing / total`

```
$ python run_tests.py
[============================] 47/52 tests passed (90.4%)

FAILED:
  tests/solutions/graph/dijkstra.fs - expected "[0, 4, 7]", got runtime error
  tests/solutions/dp/knapsack.fs - timeout (30s)
  tests/negative/freshness_002.fs - expected compile error, compiled successfully
  ...

Reward signal: 0.904
```

### 3.4 Reward Signal

```
reward = passing_tests / total_tests
```

Tracked over time in `metrics/reward_history.json`:
```json
[
    {"commit": "abc123", "timestamp": "2026-03-29T12:00:00Z", "reward": 0.0, "passing": 0, "total": 5},
    {"commit": "def456", "timestamp": "2026-03-29T13:00:00Z", "reward": 0.4, "passing": 2, "total": 5},
    ...
]
```

---

## 4. Development Process

### 4.1 Iterative Loop

```
START
  |
  v
[Pick problem from library]
  |
  v
[Can Fruit Salad express this?] --NO--> [Enhance language spec]
  |                                              |
  YES                                            |
  |  <-------------------------------------------+
  v
[Write solution in .fs + add to test suite]
  |
  v
[Write negative test cases]
  |
  v
[Run all tests locally]
  |
  v
[All pass?] --NO--> [Fix interpreter OR revise spec] --> [Run all tests]
  |
  YES
  |
  v
[Check CI status from previous commits]
  |
  v
[Fix any CI issues]
  |
  v
[Commit & push to main]
  |
  v
[More problems?] --YES--> [Pick next problem]
  |
  NO
  |
  v
[Write self-hosting compiler (ffs) in Fruit Salad]
  |
  v
DONE
```

### 4.2 Implementation Order

**Phase 1: Bootstrap (Problems 1-5)**
- Implement tokenizer/lexer
- Implement parser (AST)
- Implement basic interpreter (tree-walking)
- Target: FizzBuzz, Hello World, basic arithmetic, variable binding, if/else

**Phase 2: Core Types (Problems 6-15)**
- Bowl (structs), Medley (enums)
- Basket (arrays), Pantry (maps)
- Sort expressions (pattern matching)
- Blend (functions) with recursion
- Target: Fibonacci, factorial, bubble sort, binary search, stack/queue

**Phase 3: Advanced Features (Problems 16-30)**
- Recipes (traits/interfaces)
- Seeds (generics)
- Vine (iterators)
- Smoothie operator (~>)
- Ripe/Harvest (option/result types)
- Juice/? operator
- Target: Merge sort, quicksort, BST, hash map, Dijkstra

**Phase 4: Innovative Features (Problems 31-40)**
- Freshness tracking
- Peel (type introspection)
- Ferment (async/concurrency)
- Jam/Chute (mutex/channels)
- Target: Producer-consumer, parallel map-reduce, dining philosophers

**Phase 5: Polish & Self-Hosting (Problems 41-52+)**
- Standard library completion
- Optimisation passes
- Error message quality
- Self-hosting compiler (ffs)
- Target: All remaining problems + ffs

### 4.3 Technology Stack

| Component | Technology | Rationale |
|-----------|-----------|-----------|
| Interpreter (`fs`) | Python 3.12 | Rapid iteration, easy AST manipulation |
| Test runner | Python + pytest | Familiar, good reporting |
| Hosted harness | Docker | Sandboxing, reproducible |
| Freestanding harness | QEMU (stretch) | Bare-metal verification |
| CI | GitHub Actions | Standard, free for public repos |
| Build system | Makefile + Python | Simple, portable |
| Problem library | TOML files | Human-readable, easy to parse |

### 4.4 Repository Structure

```
fruitsalad/
  PLAN.md              -- This file
  PROMPT.md            -- Original prompt
  README.md            -- Project README
  Makefile             -- Build & test commands

  spec/
    LANGUAGE.md        -- Full language specification (living document)

  fs/                  -- The interpreter
    __main__.py        -- Entry point
    lexer.py           -- Tokenizer
    parser.py          -- Parser (tokens -> AST)
    ast_nodes.py       -- AST node definitions
    interpreter.py     -- Tree-walking interpreter
    types.py           -- Type system
    freshness.py       -- Freshness/taint tracking
    stdlib/            -- Standard library (written in Python)
      io.py
      math.py
      collections.py

  problems/            -- Problem library (TOML specs)
    sorting/
    searching/
    math/
    strings/
    dp/
    graph/
    classic/
    concurrency/
    io_parsing/
    error_handling/
    type_system/

  tests/
    solutions/         -- .fs solution files (positive tests)
    negative/          -- .fs files that should fail (negative tests)
    run_tests.py       -- Test runner
    conftest.py        -- pytest configuration

  harness/
    local/             -- Local execution harness
      runner.py
    hosted/            -- Docker execution harness
      Dockerfile
      runner.py
    freestanding/      -- QEMU execution harness (stretch)
      runner.py

  metrics/
    reward_history.json

  .github/
    workflows/
      ci.yml           -- GitHub Actions CI

  ffs/                 -- Self-hosting compiler (Phase 5)
    compiler.fs        -- The compiler written in Fruit Salad
```

---

## 5. Milestones

| # | Milestone | Reward Target | Key Deliverables |
|---|-----------|--------------|------------------|
| 1 | Hello World | 1/5 (0.20) | Lexer, parser, basic interpreter, first test passing |
| 2 | Arithmetic & Control Flow | 5/10 (0.50) | Variables, if/else, loops, basic types |
| 3 | Functions & Recursion | 10/15 (0.67) | Blend, recursion, scope |
| 4 | Data Structures | 18/25 (0.72) | Bowl, Medley, Basket, Sort |
| 5 | Advanced Types | 30/35 (0.86) | Generics, Recipes, Ripe/Harvest |
| 6 | Iterators & Pipelines | 38/42 (0.90) | Vine, ~>, each |
| 7 | Innovation Features | 45/48 (0.94) | Freshness, Peel, Ferment |
| 8 | Full Test Suite | 52/52 (1.00) | All problems passing |
| 9 | Self-Hosting | N/A | ffs compiler in Fruit Salad |

---

## Appendix: No Cheating Policy

- The `fs` interpreter must genuinely parse and execute Fruit Salad source code
- Solutions must be written in valid Fruit Salad, not embedded Python/host language
- The interpreter must not have problem-specific special cases
- The self-hosting compiler must parse real Fruit Salad source
- All test outputs must come from actual program execution
