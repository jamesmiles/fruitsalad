-- @test sort-005: Quick Sort
-- @expect [1, 5, 7, 8, 9, 10]

blend quick_sort(arr) {
    if arr.len() <= 1 {
        yield arr
    }

    preserve pivot = arr[arr.len() - 1]
    fresh left = []
    fresh middle = []
    fresh right = []

    each i in 0..arr.len() {
        if arr[i] < pivot {
            left.push(arr[i])
        } else if arr[i] == pivot {
            middle.push(arr[i])
        } else {
            right.push(arr[i])
        }
    }

    fresh sorted_left = quick_sort(left)
    fresh sorted_right = quick_sort(right)

    fresh result = []
    each i in 0..sorted_left.len() {
        result.push(sorted_left[i])
    }
    each i in 0..middle.len() {
        result.push(middle[i])
    }
    each i in 0..sorted_right.len() {
        result.push(sorted_right[i])
    }

    result
}

blend main() {
    fresh arr = [10, 7, 8, 9, 1, 5]
    display(quick_sort(arr))
}
