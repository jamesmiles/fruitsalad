-- @test search-002: Binary Search
-- @expect 3

blend binary_search(arr, target) {
    fresh low = 0
    fresh high = arr.len() - 1
    fresh result = -1

    while low <= high {
        preserve mid = (low + high) / 2
        if arr[mid] == target {
            result = mid
            snap
        } else if arr[mid] < target {
            low = mid + 1
        } else {
            high = mid - 1
        }
    }
    result
}

blend main() {
    preserve arr = [1, 3, 5, 7, 9, 11]
    display(binary_search(arr, 7))
}
