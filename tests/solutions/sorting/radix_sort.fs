-- @test sort-008: Radix Sort (LSD)
-- @expect [2, 24, 45, 66, 75, 90, 170, 802]

blend get_max(arr) {
    fresh max_val = arr[0]
    each i in 1..arr.len() {
        if arr[i] > max_val {
            max_val = arr[i]
        }
    }
    max_val
}

blend counting_sort_by_digit(arr, exp) {
    preserve n = arr.len()

    -- Create count array for digits 0-9
    fresh count = []
    each i in 0..10 {
        count.push(0)
    }

    -- Count occurrences of each digit
    each i in 0..n {
        preserve digit = (arr[i] / exp) % 10
        count[digit] = count[digit] + 1
    }

    -- Convert count to cumulative count
    each i in 1..10 {
        count[i] = count[i] + count[i - 1]
    }

    -- Build output array (iterate from right to left for stability)
    fresh output = []
    each i in 0..n {
        output.push(0)
    }

    fresh i = n - 1
    while i >= 0 {
        preserve digit = (arr[i] / exp) % 10
        count[digit] = count[digit] - 1
        output[count[digit]] = arr[i]
        i = i - 1
    }

    -- Copy back to arr
    each i in 0..n {
        arr[i] = output[i]
    }
}

blend radix_sort(arr) {
    preserve max_val = get_max(arr)

    fresh exp = 1
    while max_val / exp > 0 {
        counting_sort_by_digit(arr, exp)
        exp = exp * 10
    }
}

blend main() {
    fresh arr = [170, 45, 75, 90, 802, 24, 2, 66]
    radix_sort(arr)
    display(arr)
}
