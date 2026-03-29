-- @test type-007: Higher-Order Basket Methods (Phase 3)
-- @expect [2, 4, 6, 8, 10]
-- @expect [4, 16, 36, 64, 100]
-- @expect 220

blend filter(arr, pred) {
    fresh result = []
    each i in 0..arr.len() {
        if pred(arr[i]) {
            result.push(arr[i])
        }
    }
    result
}

blend map(arr, f) {
    fresh result = []
    each i in 0..arr.len() {
        result.push(f(arr[i]))
    }
    result
}

blend reduce(arr, f, init) {
    fresh acc = init
    each i in 0..arr.len() {
        acc = f(acc, arr[i])
    }
    acc
}

blend main() {
    preserve nums = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

    preserve evens = nums ~> filter(|x| { x % 2 == 0 })
    display(evens)

    preserve squares = evens ~> map(|x| { x * x })
    display(squares)

    preserve total = squares ~> reduce(|a, b| { a + b }, 0)
    display(total)
}
