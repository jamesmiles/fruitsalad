-- @test search-001: Linear Search
-- @expect 3

blend linear_search(arr, target) {
    fresh result = -1
    each i in 0..arr.len() {
        if arr[i] == target {
            result = i
            snap
        }
    }
    result
}

blend main() {
    preserve arr = [2, 4, 6, 8, 10]
    display(linear_search(arr, 8))
}
