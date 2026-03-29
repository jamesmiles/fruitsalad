-- @test sort-002: Selection Sort
-- @expect [11, 12, 22, 25, 64]

blend main() {
    fresh arr = [64, 25, 12, 22, 11]
    preserve n = arr.len()

    each i in 0..n - 1 {
        fresh min_idx = i
        each j in i + 1..n {
            if arr[j] < arr[min_idx] {
                min_idx = j
            }
        }
        if min_idx != i {
            arr.swap(i, min_idx)
        }
    }

    display(arr)
}
