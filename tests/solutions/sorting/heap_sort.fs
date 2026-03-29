-- @test sort-006: Heap Sort
-- @expect [5, 6, 7, 11, 12, 13]

blend heapify(arr, n, i) {
    fresh largest = i
    preserve left = 2 * i + 1
    preserve right = 2 * i + 2

    if left < n && arr[left] > arr[largest] {
        largest = left
    }

    if right < n && arr[right] > arr[largest] {
        largest = right
    }

    if largest != i {
        arr.swap(i, largest)
        heapify(arr, n, largest)
    }
}

blend heap_sort(arr) {
    preserve n = arr.len()

    -- Build max heap
    fresh i = n / 2 - 1
    while i >= 0 {
        heapify(arr, n, i)
        i = i - 1
    }

    -- Extract elements one by one
    fresh end = n - 1
    while end > 0 {
        arr.swap(0, end)
        heapify(arr, end, 0)
        end = end - 1
    }
}

blend main() {
    fresh arr = [12, 11, 13, 5, 6, 7]
    heap_sort(arr)
    display(arr)
}
