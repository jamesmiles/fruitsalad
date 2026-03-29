-- @test sort-003: Insertion Sort
-- @expect [5, 6, 11, 12, 13]

blend main() {
    fresh arr = [12, 11, 13, 5, 6]
    preserve n = arr.len()

    each i in 1..n {
        preserve key = arr[i]
        fresh j = i - 1
        while j >= 0 && arr[j] > key {
            arr[j + 1] = arr[j]
            j = j - 1
        }
        arr[j + 1] = key
    }

    display(arr)
}
