-- @test sort-001: Bubble Sort
-- @expect [1, 2, 3, 5, 8]

blend main() {
    fresh arr = [5, 3, 8, 1, 2]
    preserve n = arr.len()

    each i in 0..n {
        each j in 0..n - 1 - i {
            if arr[j] > arr[j + 1] {
                arr.swap(j, j + 1)
            }
        }
    }

    display(arr)
}
