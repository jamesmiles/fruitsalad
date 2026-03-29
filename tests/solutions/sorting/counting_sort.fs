-- @test sort-007: Counting Sort
-- @expect [1, 2, 2, 3, 3, 4, 8]

blend counting_sort(arr) {
    -- Find max value
    fresh max_val = 0
    each i in 0..arr.len() {
        if arr[i] > max_val {
            max_val = arr[i]
        }
    }

    -- Create count array
    fresh count = []
    each i in 0..=max_val {
        count.push(0)
    }

    -- Count occurrences
    each i in 0..arr.len() {
        count[arr[i]] = count[arr[i]] + 1
    }

    -- Reconstruct sorted array
    fresh result = []
    each i in 0..=max_val {
        fresh j = 0
        while j < count[i] {
            result.push(i)
            j = j + 1
        }
    }

    result
}

blend main() {
    fresh arr = [4, 2, 2, 8, 3, 3, 1]
    display(counting_sort(arr))
}
