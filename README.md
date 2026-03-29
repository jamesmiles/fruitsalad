# Fruit Salad

A memory-safe, expression-oriented, statically-typed programming language with a fruit-themed syntax.

## Quick Start

```bash
# Run a Fruit Salad program
python3 -m fs examples/hello.fs

# Run the test suite
python3 tests/run_tests.py

# Run tests with Docker (sandboxed)
make test-hosted
```

## Hello World

```
blend main() {
    display("Hello, World!")
}
```

## Language Features

### Fruit-Themed Syntax

| Concept | Fruit Salad | Example |
|---------|------------|---------|
| Integer | `Apple` | `preserve x: Apple = 42` |
| Float | `Date` | `preserve pi: Date = 3.14` |
| String | `Banana` | `preserve name: Banana = "fruit"` |
| Boolean | `Cherry` | `preserve ok: Cherry = true` |
| Function | `blend` | `blend add(a, b) { a + b }` |
| Struct | `Bowl` | `bowl Point { x: Apple, y: Apple }` |
| Enum | `Medley` | `medley Color { Red, Blue }` |
| Pattern match | `sort` | `sort x { 0 => "zero", _ => "other" }` |
| Immutable | `preserve` | `preserve x = 42` |
| Mutable | `fresh` | `fresh x = 42` |
| Array | `Basket` | `fresh arr = [1, 2, 3]` |
| Option type | `Ripe<T>` | `ripe(42)` or `pit` |
| Result type | `Harvest` | `ripe(value)` or `rot("error")` |
| Pipeline | `~>` | `data ~> filter(f) ~> map(g)` |
| Lambda | `\|x\| { expr }` | `\|x\| { x * 2 }` |
| Type check | `peel` | `peel(42)` returns `"Apple"` |

### Innovative Features

- **Smoothie Operator** (`~>`): Pipeline composition for readable data flow
- **Freshness Tracking**: Compile-time taint analysis for data provenance
- **Ripe/Harvest**: Built-in option and result types with `?` propagation
- **Sort Expressions**: Exhaustive pattern matching with guards
- **Composting**: Deterministic resource cleanup

## Examples

### FizzBuzz

```
blend main() {
    each i in 1..=15 {
        if i % 15 == 0 { display("FizzBuzz") }
        else if i % 3 == 0 { display("Fizz") }
        else if i % 5 == 0 { display("Buzz") }
        else { display(i) }
    }
}
```

### Binary Search Tree

```
medley Tree {
    Node(value: Apple, left, right),
    Empty,
}

blend insert(tree, val) {
    sort tree {
        Tree.Empty => Tree.Node(val, Tree.Empty, Tree.Empty)
        Tree.Node(v, l, r) => {
            if val < v { Tree.Node(v, insert(l, val), r) }
            else { Tree.Node(v, l, insert(r, val)) }
        }
    }
}
```

### Pipeline Composition

```
blend main() {
    preserve result = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
        ~> filter(|x| { x % 2 == 0 })
        ~> map(|x| { x * x })
        ~> reduce(|a, b| { a + b }, 0)
    display(result)  -- 220
}
```

## Test Suite

73 tests across 12 categories covering all major algorithms and data structures:

- **Sorting**: Bubble, selection, insertion, merge, quick, heap, radix, counting
- **Searching**: Linear, binary, DFS, BFS
- **Dynamic Programming**: Fibonacci, knapsack, LCS, edit distance, coin change
- **Graph**: Dijkstra, Bellman-Ford, Floyd-Warshall, Kruskal, topological sort
- **Data Structures**: Stack, queue, linked list, BST, hash map, heap, trie
- **Classic**: FizzBuzz, two sum, towers of Hanoi, N-Queens, Game of Life
- **Type System**: Bowl methods, medley options, closures, pipelines, peel

Run the tests: `python3 tests/run_tests.py --verbose`

## Project Structure

```
fruitsalad/
  fs/               -- Interpreter (Python)
  ffs/              -- Self-hosting compiler (Fruit Salad)
  tests/            -- Test suite (.fs files)
  problems/         -- Problem library (TOML specs)
  harness/          -- Execution harnesses (local + Docker)
  .github/          -- CI workflows
```

## License

MIT
