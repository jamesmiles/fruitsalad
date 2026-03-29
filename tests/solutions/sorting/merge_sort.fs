-- @test sort-004: Merge Sort
-- @expect [3, 9, 10, 27, 38, 43, 82]

blend merge(left, right) {
    fresh result = []
    fresh i = 0
    fresh j = 0

    while i < left.len() && j < right.len() {
        if left[i] <= right[j] {
            result.push(left[i])
            i = i + 1
        } else {
            result.push(right[j])
            j = j + 1
        }
    }

    while i < left.len() {
        result.push(left[i])
        i = i + 1
    }

    while j < right.len() {
        result.push(right[j])
        j = j + 1
    }

    result
}

blend merge_sort(arr) {
    if arr.len() <= 1 {
        yield arr
    }

    preserve mid = arr.len() / 2

    fresh left = []
    each i in 0..mid {
        left.push(arr[i])
    }

    fresh right = []
    each i in mid..arr.len() {
        right.push(arr[i])
    }

    fresh sorted_left = merge_sort(left)
    fresh sorted_right = merge_sort(right)

    merge(sorted_left, sorted_right)
}

blend main() {
    fresh arr = [38, 27, 43, 3, 9, 82, 10]
    display(merge_sort(arr))
}
